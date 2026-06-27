using System;
using System.Linq;
using System.Reflection;

var path = "/home/kalle/.local/share/Steam/steamapps/common/MIMESIS/MIMESIS_Data/Managed/Assembly-CSharp.dll";
var asm = Assembly.LoadFrom(path);
var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
foreach (var tn in new[] { "IVroom", "VWaitingRoom", "MaintenanceRoom", "VRoomManager", "GameSessionInfo", "FishySteamworks.Server.ServerSocket", "FishyNet.Transporting.Server.ServerSocket" }) {
    var t = asm.GetType(tn);
    Console.WriteLine($"Type {tn}: {(t != null ? "FOUND" : "MISSING")}");
    if (t == null) continue;
    if (tn is "IVroom" or "VWaitingRoom" or "MaintenanceRoom") {
        var m = t.GetMethod("CanEnterChannel", flags);
        Console.WriteLine($"  CanEnterChannel: {(m != null ? m.DeclaringType?.Name + "." + m.Name : "MISSING")}");
    }
    if (tn == "VRoomManager") {
        foreach (var n in new[] { "EnterWaitingRoom", "EnterMaintenenceRoom", "EnterMaintenanceRoom" })
            Console.WriteLine($"  {n}: {(t.GetMethod(n, flags) != null ? "FOUND" : "MISSING")}");
    }
    if (tn == "GameSessionInfo")
        Console.WriteLine($"  AddPlayerSteamID: {(t.GetMethod("AddPlayerSteamID", flags) != null ? "FOUND" : "MISSING")}");
    if (tn.Contains("ServerSocket")) {
        foreach (var mn in new[] { "GetMaximumClients", "SetMaximumClients" })
            Console.WriteLine($"  {mn}: {(t.GetMethod(mn, flags) != null ? "FOUND" : "MISSING")}");
    }
}
var steam = asm.GetTypes().FirstOrDefault(t => t.Name == "SteamInviteDispatcher");
Console.WriteLine($"SteamInviteDispatcher: {(steam != null ? steam.FullName : "MISSING")}");
if (steam != null)
    Console.WriteLine($"  CreateLobby: {(steam.GetMethod("CreateLobby", flags) != null ? "FOUND" : "MISSING")}");
var iv = asm.GetType("IVroom");
if (iv != null)
    foreach (var f in iv.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        if (f.Name.Contains("MaxPlayer"))
            Console.WriteLine($"IVroom field {f.Name}: {f.FieldType.Name}");
