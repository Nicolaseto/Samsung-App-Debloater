using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SamsungDebloater {
 public sealed class Package {
  public string Id, Path, Version, State, Action="MANTER"; public int Enabled, Uid; public bool Installed, System, Locked; public Info Info;
  public string Stamp {get {return Id+"|"+Path+"|"+Version+"|"+Installed+"|"+Enabled+"|"+Uid+"|"+System;}}
 }
 public sealed class Inventory {public string Serial, Model, Android, Fingerprint, Created, Manufacturer, Codename; public int Sdk; public List<Package> Packages=new List<Package>();}
 public sealed class Change {public string Id, Status="PENDENTE", Error; public bool BeforeInstalled, AfterInstalled; public int BeforeEnabled, AfterEnabled;}
 public sealed class Snapshot {public int Schema=2; public string Kind, Created, Serial, Fingerprint; public Inventory Inventory; public List<Change> Changes=new List<Change>();}
 public interface IAdb {Task<string> Run(string serial,string args);}
 public sealed class Adb : IAdb {
  readonly string executable; readonly Action<string> log;
  public Adb(string path,Action<string> output){executable=path;log=output;}
  public async Task<string> Run(string serial,string args){
   if(serial!=null && !Regex.IsMatch(serial,@"^[A-Za-z0-9_-]+$"))throw new Exception("Serial USB inválido.");
   var command=(serial==null?"":"-s "+serial+" ")+args;log("adb "+command);
   var result=await Task.Run(()=>{using(var p=new Process()){
    p.StartInfo=new ProcessStartInfo(executable,command){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};p.Start();
    var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();
    if(!p.WaitForExit(60000)){try{p.Kill();}catch{}throw new Exception("ADB não respondeu em 60 segundos. O estado pode ser incerto; consulte o snapshot.");}
    Task.WaitAll(stdout,stderr);return new[]{p.ExitCode.ToString(),stdout.Result,stderr.Result};
   }});
   log(result[1]+result[2]);
   if(result[0]!="0" || Regex.IsMatch(result[1]+result[2],@"(?im)^\s*(Failure|Error[: ]|Exception|Security exception|Unknown command)"))throw new Exception("Falha ADB: "+result[1]+result[2]);
   return result[1].Trim();
  }
 }
 public sealed class Store {
  public readonly string Root; readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=Int32.MaxValue};
  public Store(string root){Root=root;Directory.CreateDirectory(root);Directory.CreateDirectory(Path.Combine(root,"backups"));Directory.CreateDirectory(Path.Combine(root,"logs"));}
  public static string Key(){return DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);}
  public string Save(Snapshot value,string path=null){if(path==null)path=Path.Combine(Root,"backups",Key()+"-"+value.Kind+".json");var temp=path+".tmp";File.WriteAllText(temp,json.Serialize(value),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);return path;}
  public Snapshot Read(string path){var s=json.Deserialize<Snapshot>(File.ReadAllText(path));if(s==null||s.Schema!=2||s.Inventory==null||s.Changes==null)throw new Exception("Snapshot incompatível. Use um snapshot criado nesta versão (formato 2).");return s;}
  public Inventory Clone(Inventory i){return json.Deserialize<Inventory>(json.Serialize(i));}
 }
 public sealed class Engine {
  readonly IAdb adb; readonly Store store; readonly Action<string> log; public string LastSnapshot;
  public Engine(IAdb a,Store s,Action<string> output){adb=a;store=s;log=output;}
  public static string Detect(string text){var lines=text.Split('\n').Select(x=>x.Trim()).Where(x=>Regex.IsMatch(x,@"^\S+\s+(device|offline|unauthorized)(\s|$)")).ToArray();if(lines.Length!=1 || !Regex.IsMatch(lines[0],@"^\S+\s+device(\s|$)"))throw new Exception("Conecte exatamente um celular USB autorizado. Se aparecer unauthorized, aceite a chave RSA na tela.\r\n"+text);var serial=Regex.Split(lines[0],@"\s+")[0];if(!Regex.IsMatch(serial,@"^[A-Za-z0-9_-]+$"))throw new Exception("Este programa aceita somente conexão USB.");return serial;}
  public static HashSet<string> Ids(string text){var result=new HashSet<string>(StringComparer.Ordinal);foreach(var line in text.Split('\n').Select(x=>x.Trim()).Where(x=>x.Length>0)){if(!line.StartsWith("package:"))throw new Exception("Resposta inesperada ao listar pacotes: "+line);var id=line.Substring(8);if(!ValidId(id)||!result.Add(id))throw new Exception("Identificador de pacote inválido ou repetido.");}return result;}
  public static bool ValidId(string id){return id!=null&&Regex.IsMatch(id,@"^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)*$");}
  public static List<Package> Parse(string installedText,string systemText,string disabledText,string pathsText,string dump){
   var installed=Ids(installedText);var system=Ids(systemText);var disabled=Ids(disabledText);if(installed.Count==0)throw new Exception("Inventário vazio.");
   var paths=new Dictionary<string,string>();foreach(var line in pathsText.Split('\n').Select(x=>x.Trim()).Where(x=>x.StartsWith("package:"))){int equal=line.LastIndexOf('=');if(equal<9)throw new Exception("Caminho de pacote inválido.");paths[line.Substring(equal+1)]=line.Substring(8,equal-8);}
   var states=new Dictionary<string,Package>();
   foreach(Match m in Regex.Matches(dump,@"(?ms)^  Package \[([^\]]+)\][^\r\n]*\r?\n(.*?)(?=^  Package \[|^Hidden system packages:|^Queries:|^Dexopt state:|\z)")){
    string id=m.Groups[1].Value,block=m.Groups[2].Value;if(states.ContainsKey(id))continue;
    var user=Regex.Match(block,@"(?m)^\s+User 0: ([^\r\n]+)");var en=Regex.Match(user.Value,@"\benabled=(\d+)");var ins=Regex.Match(user.Value,@"\binstalled=(true|false)");var uid=Regex.Match(block,@"(?m)^\s+(?:userId|appId)=(\d+)");var version=Regex.Match(block,@"\bversionCode=(\d+)");
    if(!en.Success||!ins.Success||!uid.Success||!version.Success)continue;
    int enabled=int.Parse(en.Groups[1].Value);if(enabled>4)continue;
    states[id]=new Package {Id=id,Enabled=enabled,Installed=ins.Groups[1].Value=="true",Uid=int.Parse(uid.Groups[1].Value),Version=version.Groups[1].Value};
   }
   var list=new List<Package>();
   foreach(string id in installed.OrderBy(x=>x,StringComparer.Ordinal)){
    Package p;if(!states.TryGetValue(id,out p)||!p.Installed||!paths.ContainsKey(id))throw new Exception("Estado detalhado incompleto para "+id+". Nenhuma alteração permitida.");
    p.System=system.Contains(id);p.Path=paths[id];p.State=disabled.Contains(id)?"DESATIVADO":"ATIVO";p.Info=Catalog.Get(id);p.Locked=p.Info.Locked||p.Uid<10000;
    if(p.Enabled>=2&&!disabled.Contains(id))throw new Exception("Inventário inconsistente: "+id);
    if(p.Uid<10000){p.Info.Reason="UID privilegiado do sistema; não alterar.";p.Info.Risk="Crítico";}
    list.Add(p);
   }
   // Fail closed for optional packages sharing a UID with preserved services.
   foreach(var group in list.GroupBy(p=>p.Uid).Where(g=>g.Count()>1 && g.Any(p=>p.Locked)))foreach(var p in group){p.Locked=true;p.Info.Reason="UID compartilhado com componente preservado: "+String.Join(", ",group.Where(x=>x.Id!=p.Id).Select(x=>x.Id));}
   return list;
  }
  public async Task<Inventory> Analyze(bool persist=true){
   string serial=Detect(await adb.Run(null,"devices -l"));var i=new Inventory {Serial=serial,Created=DateTime.Now.ToString("O")};
   i.Model=await adb.Run(serial,"shell getprop ro.product.model");i.Android=await adb.Run(serial,"shell getprop ro.build.version.release");i.Fingerprint=await adb.Run(serial,"shell getprop ro.build.fingerprint");
   i.Manufacturer=await adb.Run(serial,"shell getprop ro.product.manufacturer");i.Codename=await adb.Run(serial,"shell getprop ro.build.version.codename");int.TryParse(await adb.Run(serial,"shell getprop ro.build.version.sdk"),out i.Sdk);
   if(String.IsNullOrWhiteSpace(i.Fingerprint))throw new Exception("Fingerprint indisponível.");
   string installed=await adb.Run(serial,"shell pm list packages --user 0"),system=await adb.Run(serial,"shell pm list packages -s --user 0"),disabled=await adb.Run(serial,"shell pm list packages -d --user 0"),paths=await adb.Run(serial,"shell pm list packages -f --user 0"),dump=await adb.Run(serial,"shell dumpsys package");
   i.Packages=Parse(installed,system,disabled,paths,dump);
   if(persist){LastSnapshot=store.Save(NewSnapshot(i,"analise"));File.WriteAllText(Path.Combine(store.Root,"pacotes-samsung.txt"),installed+Environment.NewLine,Encoding.UTF8);File.WriteAllText(Path.ChangeExtension(LastSnapshot,"paths.txt"),paths,Encoding.UTF8);File.WriteAllText(Path.ChangeExtension(LastSnapshot,"details.txt"),dump,Encoding.UTF8);}
   return i;
  }
  public static void Profile(Inventory i,string profile){foreach(var p in i.Packages){p.Action=p.Locked||profile=="Personalizado"?"MANTER":profile=="Gaming Seguro"?p.Info.Safe:p.Info.Extreme;if(!p.System&&p.Action=="REMOVER")p.Action="DESATIVAR";}}
  public static List<Package> CleanupCandidates(Inventory i){return i.Packages.Where(p=>p.Installed&&!p.Locked&&p.Uid>=10000&&!Catalog.Get(p.Id).Locked).OrderBy(p=>p.Info.Name).ToList();}
  public static string CleanupAction(Package p){return p.System&&Catalog.Get(p.Id).Removable&&p.Id!="com.android.vending"?"REMOVER":"DESATIVAR";}
  public Inventory CleanupSelection(Inventory current,IEnumerable<string> selected){
   CheckModel(current);var ids=new HashSet<string>(selected,StringComparer.Ordinal);var allowed=new HashSet<string>(CleanupCandidates(current).Select(p=>p.Id),StringComparer.Ordinal);
   if(!ids.IsSubsetOf(allowed))throw new Exception("A seleção contém aplicativo ausente ou protegido.");
   var copy=store.Clone(current);foreach(var p in copy.Packages)p.Action=ids.Contains(p.Id)?CleanupAction(p):"MANTER";return copy;
  }
  public static void Validate(Package p,string action){
   if(!ValidId(p.Id)||!new[]{"MANTER","DESATIVAR","REMOVER"}.Contains(action))throw new Exception("Ação inválida.");
   if(action=="MANTER")return;
   var info=Catalog.Get(p.Id);if(p.Locked||info.Locked||p.Uid<10000)throw new Exception("Protegido: "+p.Id);
   if(action=="REMOVER"&&(!p.System||!info.Removable||p.Id=="com.android.vending"))throw new Exception("Apenas desativação permitida: "+p.Id);
  }
  public static List<Change> Plan(Inventory i){CheckModel(i);var list=new List<Change>();foreach(var p in i.Packages){Validate(p,p.Action);if(p.Action=="MANTER"||(p.Action=="DESATIVAR"&&p.State=="DESATIVADO"))continue;list.Add(new Change{Id=p.Id,BeforeInstalled=true,BeforeEnabled=p.Enabled,AfterInstalled=p.Action!="REMOVER",AfterEnabled=p.Action=="DESATIVAR"?3:p.Enabled});}return list;}
  public static string Compatibility(Inventory i){
   if(i==null||!String.Equals(i.Manufacturer,"samsung",StringComparison.OrdinalIgnoreCase)||String.IsNullOrWhiteSpace(i.Model))return "Somente leitura: fabricante Samsung não confirmado.";
   if(i.Sdk<31||i.Sdk>37)return "Somente leitura: requer Android 12 a 17 (API 31 a 37).";
   int major;var release=(i.Android??"").Split('.')[0];
   if(!int.TryParse(release,out major)||major!=(i.Sdk<=32?12:i.Sdk-20))return "Somente leitura: versão Android e API inconsistentes.";
   if(i.Codename!="REL")return "Somente leitura: versão de prévia ou metadados incompletos.";
   return null;
  }
  public static void CheckModel(Inventory i){var reason=Compatibility(i);if(reason!=null)throw new Exception(reason);}
  static Snapshot NewSnapshot(Inventory i,string kind){return new Snapshot{Kind=kind,Created=DateTime.Now.ToString("O"),Serial=i.Serial,Fingerprint=i.Fingerprint,Inventory=i};}
  public static string EnabledCommand(int enabled){switch(enabled){case 0:return "default-state";case 1:return "enable";case 2:return "disable";case 3:return "disable-user";case 4:return "disable-until-used";default:throw new Exception("Estado enabled inválido.");}}
  public static List<string> Commands(Change c){if(!ValidId(c.Id))throw new Exception("Pacote inválido.");var commands=new List<string>();if(!c.AfterInstalled){commands.Add("shell pm uninstall -k --user 0 "+c.Id);return commands;}if(!c.BeforeInstalled)commands.Add("shell cmd package install-existing --user 0 "+c.Id);commands.Add("shell pm "+EnabledCommand(c.AfterEnabled)+" --user 0 "+c.Id);return commands;}
  async Task CheckIdentity(Inventory expected){if(await adb.Run(expected.Serial,"get-state")!="device"||await adb.Run(expected.Serial,"shell getprop ro.build.fingerprint")!=expected.Fingerprint)throw new Exception("Conexão ou firmware mudou. Analise novamente.");}
  static bool Same(Inventory a,Inventory b){return a.Serial==b.Serial&&a.Fingerprint==b.Fingerprint&&a.Manufacturer==b.Manufacturer&&a.Sdk==b.Sdk&&a.Android==b.Android&&a.Codename==b.Codename&&a.Packages.Select(p=>p.Stamp).OrderBy(x=>x).SequenceEqual(b.Packages.Select(p=>p.Stamp).OrderBy(x=>x));}
  public async Task Apply(Inventory expected,List<Change> confirmed){
   var plan=Plan(expected);if(!SamePlan(plan,confirmed))throw new Exception("Plano mudou depois da confirmação.");
   var fresh=await Analyze(false);if(!Same(expected,fresh))throw new Exception("Inventário mudou. Analise e confirme novamente.");
   foreach(var c in plan)Validate(fresh.Packages.Single(p=>p.Id==c.Id),c.AfterInstalled?"DESATIVAR":"REMOVER");
   await Transact(fresh,plan,"otimizacao");
  }
  static bool SamePlan(List<Change> a,List<Change> b){return a.Count==b.Count&&a.Zip(b,(x,y)=>x.Id==y.Id&&x.BeforeInstalled==y.BeforeInstalled&&x.AfterInstalled==y.AfterInstalled&&x.BeforeEnabled==y.BeforeEnabled&&x.AfterEnabled==y.AfterEnabled).All(x=>x);}
  public List<Change> RestorePlan(Snapshot s,Inventory now,IEnumerable<string> selected){
   if(s.Schema!=2||s.Serial!=now.Serial||s.Fingerprint!=now.Fingerprint)throw new Exception("Snapshot de outro aparelho ou firmware. Restauração bloqueada.");
   var changes=new List<Change>();var ids=new HashSet<string>(selected);if(s.Changes.Select(c=>c.Id).Distinct().Count()!=s.Changes.Count)throw new Exception("Snapshot contém entradas duplicadas.");
   foreach(var c in s.Changes.AsEnumerable().Reverse().Where(c=>ids.Contains(c.Id)&&c.Status!="PENDENTE")){
    if(!ValidId(c.Id)||c.BeforeEnabled<0||c.BeforeEnabled>4||c.AfterEnabled<0||c.AfterEnabled>4)throw new Exception("Snapshot inválido.");
    var p=now.Packages.SingleOrDefault(x=>x.Id==c.Id);bool installed=p!=null;int enabled=installed?p.Enabled:c.AfterEnabled;
    var original=s.Inventory.Packages.SingleOrDefault(x=>x.Id==c.Id);
    if((original==null && c.BeforeInstalled)||(original!=null&&(!c.BeforeInstalled||original.Enabled!=c.BeforeEnabled)))throw new Exception("Snapshot inconsistente com o estado original.");
    if(installed==c.BeforeInstalled && (!installed||enabled==c.BeforeEnabled))continue;
    if(installed!=c.AfterInstalled || (installed&&enabled!=c.AfterEnabled)){
     if(c.Status!="INICIADO" && !c.Status.StartsWith("INCERTO"))throw new Exception("Estado de "+c.Id+" diverge da operação. Restaure os snapshots mais recentes primeiro.");
    }
    if(installed && original!=null && (p.Version!=original.Version||p.Path!=original.Path))throw new Exception("Aplicativo atualizado desde o snapshot: "+c.Id);
    var info=Catalog.Get(c.Id);if(info.Locked)throw new Exception("O snapshot tenta alterar pacote protegido: "+c.Id);
    if(p!=null&&p.Locked)throw new Exception("Pacote atualmente protegido: "+c.Id);
    if(!c.BeforeInstalled && (p==null||!p.System||!info.Removable))throw new Exception("Não é possível desfazer esta restauração com segurança.");
    if(!installed && (original==null||!original.System))throw new Exception("O APK original não é de sistema; reinstalação não garantida.");
    changes.Add(new Change{Id=c.Id,BeforeInstalled=installed,BeforeEnabled=enabled,AfterInstalled=c.BeforeInstalled,AfterEnabled=c.BeforeEnabled});
   }
   return changes;
  }
  public async Task Restore(Snapshot s,Inventory expected,List<Change> confirmed){var fresh=await Analyze(false);if(!Same(expected,fresh))throw new Exception("Inventário mudou após confirmação.");var plan=RestorePlan(s,fresh,confirmed.Select(c=>c.Id));if(!SamePlan(plan,confirmed))throw new Exception("Plano de restauração mudou.");await Transact(fresh,plan,"restauracao");}
  async Task Transact(Inventory current,List<Change> plan,string kind){
   if(plan.Count==0)return;CheckModel(current);var snapshot=NewSnapshot(store.Clone(current),kind);snapshot.Changes=plan;LastSnapshot=store.Save(snapshot);log("Snapshot salvo: "+LastSnapshot);
   foreach(var c in snapshot.Changes){
    await CheckIdentity(current);c.Status="INICIADO";store.Save(snapshot,LastSnapshot);
    try{foreach(var command in Commands(c))await adb.Run(current.Serial,command);await Verify(current.Serial,c);c.Status="CONCLUÍDO";store.Save(snapshot,LastSnapshot);log("Concluído: "+c.Id);}
    catch(Exception ex){c.Status="INCERTO";c.Error=ex.Message;store.Save(snapshot,LastSnapshot);throw new Exception("Lote interrompido em "+c.Id+". Use Restaurar com o snapshot:\r\n"+LastSnapshot+"\r\n"+ex.Message);}
   }
  }
  async Task Verify(string serial,Change c){bool installed=Ids(await adb.Run(serial,"shell pm list packages --user 0")).Contains(c.Id);if(installed!=c.AfterInstalled)throw new Exception("Instalação final não confirmada.");if(installed){string dump=await adb.Run(serial,"shell dumpsys package "+c.Id);var match=Regex.Match(dump,@"(?m)^\s+User 0: [^\r\n]*\benabled=(\d+)");if(!match.Success||int.Parse(match.Groups[1].Value)!=c.AfterEnabled)throw new Exception("Estado enabled final não confirmado.");}}
 }
}
