using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SamsungDebloater {
 // In-memory transport: never starts adb.exe and never contacts a phone.
 public sealed class FakeAdb : IAdb {
  public readonly List<Package> Packages=new List<Package>();public readonly List<string> Writes=new List<string>();public string FailId;public bool Disconnect;public bool FailAfterWrite;public string Fingerprint="TEST-FIRMWARE", Model="SM-G780G", Release="13", Manufacturer="samsung", Codename="REL"; public int Sdk=33; public bool AppIdFormat;
  public FakeAdb(){Add("android",1000);Add("com.google.android.gm",10101);Add("com.android.vending",10102);Add("com.google.android.youtube",10103);}
  void Add(string id,int uid){Packages.Add(new Package{Id=id,Uid=uid,Installed=true,Enabled=0,System=true,Version="123",Path="/system/app/"+id+"/base.apk"});}
  string List(Func<Package,bool> f,bool path=false){return String.Join("\n",Packages.Where(p=>p.Installed&&f(p)).Select(p=>"package:"+(path?p.Path+"=":"")+p.Id));}
  string Dump(IEnumerable<Package> packages){return "Packages:\n"+String.Join("\n",packages.Select(p=>"  Package ["+p.Id+"] (abc):\n    "+(AppIdFormat?"appId":"userId")+"="+p.Uid+"\n    versionCode="+p.Version+"\n    User 0: installed="+p.Installed.ToString().ToLowerInvariant()+" hidden=false enabled="+p.Enabled+"\n"));}
  public Task<string> Run(string serial,string args){
   if(Disconnect)throw new Exception("disconnected");string result;
   if(args=="devices -l")result="List of devices attached\nTESTUSB device model:SM_G780G";
   else if(args=="get-state")result="device";
   else if(args=="shell getprop ro.product.model")result=Model;
   else if(args=="shell getprop ro.build.version.release")result=Release;
   else if(args=="shell getprop ro.product.manufacturer")result=Manufacturer;
   else if(args=="shell getprop ro.build.version.codename")result=Codename;
   else if(args=="shell getprop ro.build.version.sdk")result=Sdk.ToString();
   else if(args=="shell getprop ro.build.fingerprint")result=Fingerprint;
   else if(args=="shell pm list packages --user 0")result=List(p=>true);
   else if(args=="shell pm list packages -s --user 0")result=List(p=>p.System);
   else if(args=="shell pm list packages -d --user 0")result=List(p=>p.Enabled>=2);
   else if(args=="shell pm list packages -f --user 0")result=List(p=>true,true);
   else if(args=="shell dumpsys package")result=Dump(Packages);
   else if(args.StartsWith("shell dumpsys package "))result=Dump(Packages.Where(p=>p.Id==args.Split(' ').Last()));
   else {
    var id=args.Split(' ').Last();var p=Packages.Single(x=>x.Id==id);Writes.Add(args);if(FailId==id&&!FailAfterWrite)throw new Exception("simulated failure");
    if(args.StartsWith("shell pm uninstall -k --user 0 "))p.Installed=false;
    else if(args.StartsWith("shell cmd package install-existing --user 0 "))p.Installed=true;
    else {var verb=args.Split(' ')[2];var map=new Dictionary<string,int>{{"default-state",0},{"enable",1},{"disable",2},{"disable-user",3},{"disable-until-used",4}};if(!map.ContainsKey(verb))throw new Exception("unexpected command "+args);p.Enabled=map[verb];}
    if(FailId==id&&FailAfterWrite)throw new Exception("connection lost after write");result="Success";
   }
   return Task.FromResult(result);
  }
 }
 public static class Tests {
  static readonly List<string> Results=new List<string>();
  static void Assert(bool value,string name){if(!value)throw new Exception("FAIL: "+name);Results.Add("PASS: "+name);}
  static void Reject(Action action,string name){try{action();}catch{Results.Add("PASS: "+name);return;}throw new Exception("FAIL: "+name);}
  static async Task RejectAsync(Func<Task> action,string name){try{await action();}catch{Results.Add("PASS: "+name);return;}throw new Exception("FAIL: "+name);}
  public static async Task Run(string root,bool renderUI=true){
   Results.Clear();var area=Path.Combine(root,"test-artifacts",Store.Key());var store=new Store(area);var fake=new FakeAdb();var engine=new Engine(fake,store,s=>{});
   var real=DemoInventory.Create();Assert(real.Packages.Count>40,"parser recebe inventário sintético sem dados privados");Assert(real.Packages.Count(p=>p.State=="DESATIVADO")==4,"quatro estados desativados preservados");
   foreach(var id in new[]{"android","com.android.systemui","com.google.android.gms","com.google.android.gsf","org.fdroid.fdroid","com.aurora.store","com.samsung.android.game.gos","com.sec.android.app.desktoplauncher","com.sec.android.app.dexonpc","com.sec.android.dexsystemui","com.sec.android.desktopmode.uiservice","com.sec.android.app.myfiles","com.samsung.vklayer.sm8250","com.samsung.gamedriver.sm8250","com.qualcomm.qti.gpudrivers.kona.api30","com.google.android.permissioncontroller","com.google.android.packageinstaller","com.samsung.android.smartmirroring","com.samsung.android.mcfserver"})Assert(real.Packages.Single(p=>p.Id==id).Locked,"proteção "+id);
   Engine.Profile(real,"S20 FE Gaming Extremo");Assert(real.Packages.Single(p=>p.Id=="com.android.vending").Action=="DESATIVAR","Play Store apenas desativada");foreach(var p in real.Packages)Engine.Validate(p,p.Action);
   MainForm.WriteReport(real,Path.Combine(root,"classificacao-samsung.csv"));Results.Add("INFO: Extremo "+String.Join(", ",real.Packages.GroupBy(p=>p.Action).Select(g=>g.Key+"="+g.Count())));
   Engine.Profile(real,"Gaming Seguro");Assert(real.Packages.All(p=>p.Action!="REMOVER"),"Gaming Seguro sem remoções");
   var safeIds=new[]{"com.facebook.appmanager","com.facebook.services","com.facebook.system","com.samsung.android.app.spage","com.samsung.android.dynamiclock","com.sec.android.usermanual"};
   Assert(new HashSet<string>(real.Packages.Where(p=>p.Action=="DESATIVAR").Select(p=>p.Id)).SetEquals(safeIds),"Seguro só propõe os seis aplicativos do escopo conservador");
   foreach(string id in new[]{"com.android.vending","com.android.chrome","com.google.android.gm","com.google.android.apps.maps","com.samsung.android.samsungpass","com.samsung.android.samsungpassautofill","com.samsung.android.calendar","com.samsung.android.app.reminder","com.microsoft.skydrive","com.microsoft.appmanager","com.samsung.android.arzone"})Assert(real.Packages.Single(p=>p.Id==id).Action=="MANTER","Seguro preserva "+id);
   Assert(real.Packages.Count(p=>p.Action=="MANTER")==real.Packages.Count-6,"Seguro mantém os demais pacotes");
   Assert(real.Packages.Single(p=>p.Id=="com.osp.app.signin").Info.Name=="Samsung Account"&&real.Packages.Single(p=>p.Id=="com.osp.app.signin").Locked,"Samsung Account identificado e protegido");
   Assert(Catalog.Get("com.unknown.future.package").Locked,"pacote desconhecido continua bloqueado");
   Assert(real.Packages.Where(p=>p.Id.StartsWith("org.example.")).All(p=>p.Info.Name=="Componente não documentado"&&p.Locked),"identificadores desconhecidos permanecem não documentados e bloqueados");
   var candidates=Engine.CleanupCandidates(real);Assert(candidates.Any(p=>p.Id=="com.netflix.mediaclient")&&candidates.Any(p=>p.Id=="com.google.android.youtube"),"Netflix e YouTube pré-instalados disponíveis na limpeza");
   Assert(new HashSet<string>(candidates.Select(p=>p.Id)).SetEquals(real.Packages.Where(p=>p.Installed&&!p.Locked).Select(p=>p.Id)),"limpeza mostra todos os instalados não protegidos");
   Assert(candidates.Count>0&&candidates.Any(p=>p.Id=="com.android.chrome"),"aplicativos que só permitem desativar também aparecem");
   foreach(string profileName in new[]{"Gaming Seguro","S20 FE Gaming Extremo","Personalizado"}){Engine.Profile(real,profileName);Assert(real.Packages.Single(p=>p.Id=="com.netflix.mediaclient").Action=="MANTER","Netflix não adicionada automaticamente ao perfil "+profileName);}
   Engine.Profile(real,"Gaming Seguro");
   if(renderUI)using(var cleanupDialog=new CleanupDialog(candidates)){cleanupDialog.VerifyAndCapture(Path.Combine(root,"limpeza-preview.png"));Assert(cleanupDialog.SelectedIds.Count==0,"tela de limpeza começa desmarcada e permite desmarcar");}
   MainForm.WriteReport(real,Path.Combine(root,"classificacao-samsung-seguro.csv"));
   Engine.Profile(real,"Personalizado");Assert(real.Packages.All(p=>p.Action=="MANTER"),"Personalizado inicia sem mudanças");
   Reject(()=>Engine.Detect("List of devices attached\nX unauthorized"),"ADB não autorizado");Reject(()=>Engine.Detect("List of devices attached"),"ADB sem dispositivo");Reject(()=>Engine.Detect("A device\nB device"),"ADB com múltiplos dispositivos");Reject(()=>Engine.Detect("127.0.0.1:5555 device"),"conexão não USB bloqueada");
   Reject(()=>Engine.Ids("package:bad;reboot"),"injeção no identificador bloqueada");Reject(()=>Engine.Ids("Failure [ERROR]"),"lista inválida bloqueada");
   foreach(int sdk in new[]{31,32,33,34,35,36,37}){
    var transport=new FakeAdb{Sdk=sdk,Release=(sdk<=32?12:sdk-20).ToString(),Model=sdk<34?"SM-A525F":"SM-S921B",AppIdFormat=sdk>=35};
    var multi=new Engine(transport,store,x=>{});var device=await multi.Analyze();Engine.CheckModel(device);
    Assert(transport.Writes.Count==0,"API "+sdk+": análise somente leitura");
    var chosen=multi.CleanupSelection(device,new[]{"com.google.android.youtube","com.android.vending"});await multi.Apply(chosen,Engine.Plan(chosen));var journal=store.Read(multi.LastSnapshot);
    Assert(!transport.Packages.Single(p=>p.Id=="com.google.android.youtube").Installed&&transport.Packages.Single(p=>p.Id=="com.android.vending").Enabled==3,"API "+sdk+": remoção e desativação simuladas");
    journal.Inventory.Manufacturer=null;journal.Inventory.Sdk=0;journal.Inventory.Codename=null;
    device=await multi.Analyze();await multi.Restore(journal,device,multi.RestorePlan(journal,device,journal.Changes.Select(c=>c.Id)));
    Assert(transport.Packages.All(p=>p.Installed&&p.Enabled==0),"API "+sdk+": restauração de snapshot formato 2");
   }
   foreach(var unsupported in new[]{new FakeAdb{Manufacturer="Google"},new FakeAdb{Sdk=30,Release="11"},new FakeAdb{Sdk=38,Release="18"},new FakeAdb{Sdk=0},new FakeAdb{Codename="Preview"},new FakeAdb{Release="14"},new FakeAdb{Manufacturer=""}}){
    var blocked=new Engine(unsupported,store,x=>{});var device=await blocked.Analyze();Engine.Profile(device,"Samsung Gaming Extremo");
    await RejectAsync(()=>blocked.Apply(device,new List<Change>()),"metadados incompatíveis bloqueiam execução");Assert(unsupported.Writes.Count==0,"incompatibilidade não altera pacotes");
   }
   var inv=await engine.Analyze();Assert(fake.Writes.Count==0,"análise só faz leitura");
   var vending=inv.Packages.Single(p=>p.Id=="com.android.vending");Reject(()=>Engine.Validate(vending,"REMOVER"),"remoção de Play Store bloqueada no motor");Reject(()=>Engine.Validate(inv.Packages.Single(p=>p.Id=="android"),"DESATIVAR"),"desativação de pacote protegido bloqueada");Reject(()=>Engine.Validate(vending,"REBOOT"),"ação desconhecida bloqueada");
   vending.Action="DESATIVAR";var gmail=inv.Packages.Single(p=>p.Id=="com.google.android.gm");gmail.Action="REMOVER";
   var plan=Engine.Plan(inv);await engine.Apply(inv,plan);var snapshot=store.Read(engine.LastSnapshot);Assert(snapshot.Changes.All(c=>c.Status=="CONCLUÍDO"),"journal persistido após aplicar");Assert(!fake.Packages.Single(p=>p.Id==gmail.Id).Installed&&fake.Packages.Single(p=>p.Id==vending.Id).Enabled==3,"remoção usuário 0 e desativação simuladas");
   var now=await engine.Analyze();var restore=engine.RestorePlan(snapshot,now,snapshot.Changes.Select(c=>c.Id));await engine.Restore(snapshot,now,restore);Assert(fake.Packages.All(p=>p.Installed&&p.Enabled==0),"restauração com default-state exato e install-existing");
   now=await engine.Analyze();Assert(engine.RestorePlan(snapshot,now,snapshot.Changes.Select(c=>c.Id)).Count==0,"restauração idempotente");
   snapshot.Serial="OTHER";Reject(()=>engine.RestorePlan(snapshot,now,new[]{gmail.Id}),"snapshot de outro aparelho bloqueado");snapshot.Serial=now.Serial;
   inv=await engine.Analyze();Engine.Profile(inv,"S20 FE Gaming Extremo");plan=Engine.Plan(inv);fake.Packages.Single(p=>p.Id==vending.Id).Enabled=1;int count=fake.Writes.Count;await RejectAsync(()=>engine.Apply(inv,plan),"inventário alterado bloqueia execução");Assert(fake.Writes.Count==count,"preflight não escreveu após divergência");
   fake.Packages.Single(p=>p.Id==vending.Id).Enabled=0;inv=await engine.Analyze();Engine.Profile(inv,"S20 FE Gaming Extremo");plan=Engine.Plan(inv);fake.FailId="com.google.android.gm";fake.FailAfterWrite=true;await RejectAsync(()=>engine.Apply(inv,plan),"falha após escrita interrompe lote");snapshot=store.Read(engine.LastSnapshot);Assert(snapshot.Changes.Any(c=>c.Status=="INCERTO")&&snapshot.Changes.Last().Status=="PENDENTE","journal distingue incerto e não executado");
   fake.FailId=null;now=await engine.Analyze();restore=engine.RestorePlan(snapshot,now,snapshot.Changes.Where(c=>c.Status!="PENDENTE").Select(c=>c.Id));await engine.Restore(snapshot,now,restore);Assert(fake.Packages.All(p=>p.Installed&&p.Enabled==0),"recuperação de lote parcial");
   inv=await engine.Analyze();Engine.Profile(inv,"S20 FE Gaming Extremo");plan=Engine.Plan(inv);fake.Disconnect=true;count=fake.Writes.Count;await RejectAsync(()=>engine.Apply(inv,plan),"desconexão antes do lote");Assert(fake.Writes.Count==count,"nenhuma escrita após desconexão");fake.Disconnect=false;
   fake.Packages.Single(p=>p.Id==gmail.Id).Enabled=2;inv=await engine.Analyze();inv.Packages.Single(p=>p.Id==gmail.Id).Action="REMOVER";await engine.Apply(inv,Engine.Plan(inv));snapshot=store.Read(engine.LastSnapshot);now=await engine.Analyze();await engine.Restore(snapshot,now,engine.RestorePlan(snapshot,now,new[]{gmail.Id}));Assert(fake.Packages.Single(p=>p.Id==gmail.Id).Enabled==2,"restauração preserva desativação anterior");
   fake.Packages.Add(new Package {Id="com.netflix.mediaclient",Uid=10200,Installed=true,Enabled=0,System=true,Version="123",Path="/system/app/Netflix_stub/Netflix_stub.apk"});
   inv=await engine.Analyze();Engine.Profile(inv,"S20 FE Gaming Extremo");count=fake.Writes.Count;
   var cleanup=engine.CleanupSelection(inv,new[]{"com.netflix.mediaclient"});var cleanupPlan=Engine.Plan(cleanup);
   Assert(cleanupPlan.Count==1&&cleanupPlan[0].Id=="com.netflix.mediaclient"&&!cleanupPlan[0].AfterInstalled,"limpeza aplica só a seleção e ignora outras propostas do perfil");
   Assert(inv.Packages.Single(p=>p.Id=="com.android.vending").Action=="DESATIVAR"&&inv.Packages.Single(p=>p.Id=="com.netflix.mediaclient").Action=="MANTER","montar limpeza não altera plano da tela principal");
   Assert(Engine.Plan(engine.CleanupSelection(inv,new string[0])).Count==0&&fake.Writes.Count==count,"seleção vazia não gera ações nem escritas");
   Assert(engine.CleanupSelection(inv,new[]{"com.android.vending"}).Packages.Single(p=>p.Id=="com.android.vending").Action=="DESATIVAR","Play Store aparece somente para desativar");Reject(()=>engine.CleanupSelection(inv,new[]{"android"}),"limpeza rejeita componente essencial");Reject(()=>engine.CleanupSelection(inv,new[]{"com.missing.app"}),"limpeza rejeita pacote ausente");
   await engine.Apply(cleanup,cleanupPlan);snapshot=store.Read(engine.LastSnapshot);Assert(fake.Writes.Count==count+1&&!fake.Packages.Single(p=>p.Id=="com.netflix.mediaclient").Installed,"Netflix removida isoladamente no simulador");
   now=await engine.Analyze();Assert(!Engine.CleanupCandidates(now).Any(p=>p.Id=="com.netflix.mediaclient"),"app removido desaparece da limpeza após reanálise");await engine.Restore(snapshot,now,engine.RestorePlan(snapshot,now,new[]{"com.netflix.mediaclient"}));Assert(fake.Packages.Single(p=>p.Id=="com.netflix.mediaclient").Installed,"Netflix recuperada por snapshot no simulador");
   inv=await engine.Analyze();inv.Packages.Single(p=>p.Id=="com.netflix.mediaclient").System=false;Assert(engine.CleanupSelection(inv,new[]{"com.netflix.mediaclient"}).Packages.Single(p=>p.Id=="com.netflix.mediaclient").Action=="DESATIVAR","app fora do sistema aparece para desativar, sem remoção não recuperável");
   var mixedFake=new FakeAdb();var mixedEngine=new Engine(mixedFake,store,s=>{});var mixedInventory=await mixedEngine.Analyze();var mixedSelection=mixedEngine.CleanupSelection(mixedInventory,new[]{"com.google.android.youtube","com.android.vending"});var mixedPlan=Engine.Plan(mixedSelection);
   Assert(mixedPlan.Count==2&&mixedPlan.Single(c=>c.Id=="com.android.vending").AfterInstalled&&!mixedPlan.Single(c=>c.Id=="com.google.android.youtube").AfterInstalled,"limpeza combina remoção e desativação explicitamente");
   await mixedEngine.Apply(mixedSelection,mixedPlan);var mixedSnapshot=store.Read(mixedEngine.LastSnapshot);mixedInventory=await mixedEngine.Analyze();await mixedEngine.Restore(mixedSnapshot,mixedInventory,mixedEngine.RestorePlan(mixedSnapshot,mixedInventory,mixedPlan.Select(c=>c.Id)));Assert(mixedFake.Packages.All(p=>p.Installed&&p.Enabled==0),"limpeza mista executada e restaurada no simulador");
   if(renderUI)using(var form=new MainForm()){form.VerifyPreview(Path.Combine(root,"interface-preview.png"));Assert(form.Controls.Count>0,"interface: prévia, pesquisa, filtro e troca de perfil");}
   File.WriteAllLines(Path.Combine(root,"test-results.txt"),Results); 
  }
 }
}
