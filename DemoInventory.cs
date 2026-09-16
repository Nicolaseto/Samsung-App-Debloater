using System;
using System.Linq;
using System.Collections.Generic;

namespace SamsungDebloater {
 // Synthetic data only. No serial, fingerprint or dump copied from a phone.
 public static class DemoInventory {
  public static Inventory Create(){
   var ids=Catalog.OptionalIds.Concat(new[]{"android","com.android.systemui","com.google.android.gms","com.google.android.gsf","org.fdroid.fdroid","com.aurora.store","com.samsung.android.game.gos","com.sec.android.app.desktoplauncher","com.sec.android.app.dexonpc","com.sec.android.dexsystemui","com.sec.android.desktopmode.uiservice","com.sec.android.app.myfiles","com.samsung.vklayer.sm8250","com.samsung.gamedriver.sm8250","com.qualcomm.qti.gpudrivers.kona.api30","com.google.android.permissioncontroller","com.google.android.packageinstaller","com.samsung.android.smartmirroring","com.samsung.android.mcfserver","com.osp.app.signin","org.example.unknown.one","org.example.unknown.two","org.example.unknown.three","org.example.unknown.four"}).Distinct().OrderBy(id=>id).ToList();
   Func<string,string> line=id=>"package:"+id;
   string installed=String.Join("\n",ids.Select(line));string disabled=String.Join("\n",ids.Where(id=>id.StartsWith("org.example.")).Select(line));
   string paths=String.Join("\n",ids.Select(id=>"package:/system/app/Demo/"+id+".apk="+id));
   string dump="Packages:\n"+String.Join("\n",ids.Select((id,n)=>"  Package ["+id+"] (demo):\n    userId="+(id=="android"?1000:11000+n)+"\n    versionCode=1\n    User 0: installed=true hidden=false enabled="+(id.StartsWith("org.example.")?3:0)+"\n"));
   return new Inventory{Serial="DEMO",Model="SM-G780G",Android="13",Manufacturer="samsung",Sdk=33,Codename="REL",Fingerprint="SYNTHETIC-DEMO-NOT-A-DEVICE",Created=DateTime.UtcNow.ToString("O"),Packages=Engine.Parse(installed,installed,disabled,paths,dump)};
  }
 }
}
