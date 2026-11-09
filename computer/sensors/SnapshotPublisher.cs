using System;
using System.IO;
using System.Threading;

public static class SnapshotPublisher {
 public static bool IsSharingViolation(IOException error) {
  int code=error.HResult & 0xffff;
  return code==32 || code==33;
 }

 // A reader or antivirus can briefly open the destination without delete sharing.
 // Keep atomic replacement and the existing ACLs; never fall back to truncation.
 public static bool TrySave(string file,string text,Action<string,string> writeNew) {
  string full=Path.GetFullPath(file);
  Directory.CreateDirectory(Path.GetDirectoryName(full));
  string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
  try {
   writeNew(temp,text);
   for(int attempt=0;attempt<10;attempt++) {
    try {
     if(File.Exists(full)) File.Replace(temp,full,null); else File.Move(temp,full);
     return true;
    } catch(IOException error) {
     if(!IsSharingViolation(error)) throw;
     if(attempt==9) return false;
     Thread.Sleep(50);
    }
   }
   return false;
  } catch(IOException error) {
   if(!IsSharingViolation(error)) throw;
   return false;
  } finally {
   try {if(File.Exists(temp)) File.Delete(temp);}
   catch(IOException error) {if(!IsSharingViolation(error)) throw;}
  }
 }
}
