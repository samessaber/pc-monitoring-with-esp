using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using LibreHardwareMonitor.Hardware;

class CpuSensors {
 static void Update(IHardware h) {
  h.Update();
  foreach(var child in h.SubHardware) Update(child);
 }
 static IEnumerable<ISensor> Sensors(IHardware h) {
  foreach(var s in h.Sensors) yield return s;
  foreach(var child in h.SubHardware) foreach(var s in Sensors(child)) yield return s;
 }
 static int Rank(string name) {
  string n=name.ToLowerInvariant();
  if(n.Contains("tctl") && n.Contains("tdie")) return 0;
  if(n.Contains("package")) return 1;
  if(n.Contains("tdie")) return 2;
  if(n.Contains("core") && !n.Contains("ccd")) return 3;
  return 4;
 }
 static bool Save(string file,object value) {
  return SnapshotPublisher.TrySave(file,new JavaScriptSerializer().Serialize(value),SensorFileSecurity.WriteNew);
 }
 public static int Main(string[] args) {
  bool once=args.Contains("--once");
  string file=args.FirstOrDefault(a=>!a.StartsWith("--")) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cpu-sensors.json");
  Mutex mutex=null;bool first;
  if(!once) {
   mutex=new Mutex(true,"Local\\ESP32CpuSensors",out first);
   if(!first) {mutex.Dispose();return 0;}
  }
  var computer=new Computer { IsCpuEnabled=true };
  try {
   computer.Open();
   do {
    var values=new List<Dictionary<string,object>>();
    var candidates=new List<ISensor>();
    var powerValues=new List<Dictionary<string,object>>();
    var packagePower=new List<ISensor>();
    string cpu="";
    foreach(var hardware in computer.Hardware.Where(h=>h.HardwareType==HardwareType.Cpu)) {
     Update(hardware);cpu=hardware.Name;
     foreach(var sensor in Sensors(hardware).Where(s=>s.SensorType==SensorType.Temperature)) {
      values.Add(new Dictionary<string,object>{{"name",sensor.Name},{"value",sensor.Value}});
      if(sensor.Value.HasValue && sensor.Value.Value>0 && sensor.Value.Value<=125) candidates.Add(sensor);
     }
     foreach(var sensor in Sensors(hardware).Where(s=>s.SensorType==SensorType.Power)) {
      powerValues.Add(new Dictionary<string,object>{{"name",sensor.Name},{"value",sensor.Value}});
      if(PowerSelection.IsPackagePower(sensor.Name,sensor.Value))
       packagePower.Add(sensor);
     }
    }
    var chosen=candidates.OrderBy(s=>Rank(s.Name)).ThenByDescending(s=>s.Value).FirstOrDefault();
    var result=new Dictionary<string,object> {
     {"cpu_name",cpu},
     {"cpu_temperature_c",chosen==null ? null : (object)chosen.Value.Value},
     {"sensor_name",chosen==null ? null : chosen.Name},
     {"cpu_power_w",packagePower.Count==1 ? (object)packagePower[0].Value.Value : null},
     {"power_sensor_name",packagePower.Count==1 ? packagePower[0].Name : null},
     {"power_sensors",powerValues},
     {"updated_at_unix",DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
     {"temperature_sensors",values},
     {"source","LibreHardwareMonitor 0.9.6"}
    };
    // A persistent sharing lock skips this sample, not the sensor process.
    // The next iteration samples again; consumers still reject stale data.
    if(!Save(file,result) && once) return 2;
    if(once) Console.WriteLine(new JavaScriptSerializer().Serialize(result));
    if(!once) Thread.Sleep(2000);
   } while(!once);
   return 0;
  } catch(Exception e) {
   try {Save(file,new Dictionary<string,object>{{"cpu_temperature_c",null},{"cpu_power_w",null},{"error",e.ToString()},{"updated_at_unix",DateTimeOffset.UtcNow.ToUnixTimeSeconds()}});}
   catch(IOException) {} catch(UnauthorizedAccessException) {}
   if(once) Console.Error.WriteLine(e);
   return 1;
  } finally {computer.Close();if(mutex!=null) mutex.Dispose();}
 }
}
