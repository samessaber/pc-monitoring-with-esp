using System;
using System.IO;
using System.Threading;

class SnapshotPublisherTests {
 static void Check(bool condition,string message) {
  if(!condition) throw new Exception(message);
 }
 static int Main(string[] args) {
  string directory=Path.Combine(Path.GetFullPath(args[0]),Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(directory);
  string snapshot=Path.Combine(directory,"snapshot.json");
  try {
   Check(SnapshotPublisher.TrySave(snapshot,"old",File.WriteAllText),"Initial publication failed");
   var held=new FileStream(snapshot,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
   var release=new Thread(delegate() {Thread.Sleep(120);held.Dispose();});
   release.Start();
   try {Check(SnapshotPublisher.TrySave(snapshot,"short-lock-recovered",File.WriteAllText),"Short lock was not retried");}
   finally {release.Join();held.Dispose();}
   Check(File.ReadAllText(snapshot)=="short-lock-recovered","Short-lock update missing");

   using(var longLock=new FileStream(snapshot,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {
    Check(!SnapshotPublisher.TrySave(snapshot,"must-not-publish",File.WriteAllText),"Locked target incorrectly reported success");
    Check(File.ReadAllText(snapshot)=="short-lock-recovered","Lock failure damaged last complete snapshot");
   }
   Check(SnapshotPublisher.TrySave(snapshot,"next-sample",File.WriteAllText),"Publication did not recover after a long lock");
   Check(File.ReadAllText(snapshot)=="next-sample","Recovered snapshot missing");
   Check(Directory.GetFiles(directory,"*.tmp").Length==0,"Temporary files leaked");

   bool denied=false;
   try {SnapshotPublisher.TrySave(snapshot,"denied",delegate(string path,string text) {throw new UnauthorizedAccessException();});}
   catch(UnauthorizedAccessException) {denied=true;}
   Check(denied,"Permission error was treated as a sharing lock");

   string blocked=Path.Combine(directory,"directory-instead-of-file");
   Directory.CreateDirectory(blocked);
   bool nonSharingError=false;
   try {SnapshotPublisher.TrySave(blocked,"bad-target",File.WriteAllText);}
   catch(IOException error) {nonSharingError=!SnapshotPublisher.IsSharingViolation(error);}
   catch(UnauthorizedAccessException) {nonSharingError=true;}
   Check(nonSharingError,"Non-sharing failure was swallowed");
   Directory.Delete(blocked);
   Console.WriteLine("PASS: short-lock retry, long-lock skip/recovery, atomic snapshot preservation, cleanup, and non-sharing failures.");
   return 0;
  } finally {
   foreach(string file in Directory.GetFiles(directory)) File.Delete(file);
   if(Directory.GetDirectories(directory).Length==0) Directory.Delete(directory);
  }
 }
}
