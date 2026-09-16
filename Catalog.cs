using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SamsungDebloater {
 public sealed class Info {
  public string Name, Description, Risk, Reason, Vendor;
  public bool Locked = true, Removable;
  public string Extreme = "MANTER", Safe = "MANTER";
 }
 public static class Catalog {
  // Exact allowlist drawn from pacotes-s20fe.txt. Unknown/new IDs fail closed.
  static readonly Dictionary<string, Info> Optional = Build();
  public static IEnumerable<string> OptionalIds {get{return Optional.Keys.OrderBy(id=>id);}}
  static Dictionary<string, Info> Build() {
   var d = new Dictionary<string, Info>(StringComparer.Ordinal);
   Action<string,string,string,bool> add = (id,name,impact,remove) => d.Add(id,new Info {Name=name,Description=impact,Risk="Moderado",Locked=false,Removable=remove,Extreme=remove?"REMOVER":"DESATIVAR",Safe="MANTER",Reason="Aplicativo opcional identificado no inventário; perde a função descrita."});
   add("com.android.chrome","Chrome","Navegador e abas de autenticação. Sem outro navegador, links e login externo de jogos podem falhar.",false);
   add("com.android.vending","Google Play Store","Loja, atualizações e verificação de licenças. Desativar pode impedir a abertura ou compra em alguns jogos.",false);
   add("com.google.android.gm","Gmail","Cliente de e-mail Gmail; notificações e leitura de e-mail deixam de funcionar.",true);
   add("com.google.android.apps.maps","Google Maps","Mapas e navegação. Aplicativos que abrem o Maps precisarão de uma alternativa.",true);
   add("com.google.android.apps.tachyon","Google Meet / Duo","Chamadas e reuniões do Google; perde chamadas por este aplicativo.",true);
   add("com.google.android.youtube","YouTube","Aplicativo de vídeos; links poderão precisar de navegador.",true);
   add("com.netflix.mediaclient","Netflix","Aplicativo de filmes e séries. Ao remover, você perde o acesso pelo app neste usuário; não cancela sua assinatura. Preserve se usa sua integração com jogos Netflix.",true);
   d["com.netflix.mediaclient"].Extreme="MANTER"; // Optional cleanup only, never added to an automatic profile proposal.
   add("com.google.android.googlequicksearchbox","Pesquisa Google / assistente","Pesquisa, widgets e integração com assistente. Afeta atalhos de voz.",false);
   add("com.android.hotwordenrollment.okgoogle","Cadastro de voz OK Google","Integração de treinamento de voz do assistente. Reconhecimento por palavra-chave pode parar.",false);
   add("com.android.hotwordenrollment.xgoogle","Cadastro de voz Google","Integração de treinamento de voz do assistente. Reconhecimento por palavra-chave pode parar.",false);
   add("com.facebook.appmanager","Meta App Manager","Gerencia aplicativos Meta pré-instalados; perde atualizações e integração fornecidas por esse serviço.",true);
   add("com.facebook.services","Meta Services","Serviços de integração Meta; recursos dos aplicativos Meta podem ser afetados.",true);
   add("com.facebook.system","Meta App Installer","Instalador auxiliar dos aplicativos Meta pré-instalados.",true);
   add("com.microsoft.appmanager","Vincular ao Windows","Integração com o computador: chamadas, mensagens e notificações. Não é o Samsung DeX.",false);
   add("com.microsoft.skydrive","OneDrive","Sincronização e acesso a arquivos Microsoft. Backup da Galeria pode depender dele.",false);
   add("com.samsung.android.bixby.agent","Bixby","Assistente Samsung; perde comandos e ações do Bixby.",true);
   add("com.samsung.android.bixby.wakeup","Ativação de voz do Bixby","Ativação por voz do assistente Samsung.",true);
   add("com.samsung.android.app.settings.bixby","Configurações do Bixby","Integração de configurações do Bixby; atalhos associados podem deixar de funcionar.",false);
   add("com.samsung.android.app.spage","Samsung Free / painel de conteúdo","Painel lateral de notícias e conteúdo da Samsung.",true);
   add("com.samsung.android.arzone","AR Zone","Central de recursos de realidade aumentada; seus atalhos na câmera podem parar.",false);
   add("com.samsung.android.aremoji","AR Emoji","Criação de avatares e emojis; afeta modos relacionados da câmera.",false);
   add("com.samsung.android.ardrawing","AR Doodle","Desenho sobre imagem em realidade aumentada; recurso da câmera deixa de funcionar.",false);
   add("com.samsung.android.kidsinstaller","Samsung Kids","Instalação e acesso ao ambiente infantil Samsung.",true);
   add("com.samsung.android.calendar","Samsung Calendar","Interface de agenda Samsung. O provedor de calendário permanece protegido.",false);
   add("com.samsung.android.app.reminder","Samsung Reminder","Lembretes e seus alertas deixam de funcionar.",false);
   add("com.samsung.android.app.routines","Modos e Rotinas","Automações Samsung, incluindo rotinas de jogos ou de conexão com DeX.",false);
   add("com.samsung.android.scloud","Samsung Cloud","Backup e sincronização Samsung. Não desative se depender desse backup.",false);
   add("com.samsung.android.samsungpass","Samsung Pass","Credenciais e autenticação Samsung Pass; exporte ou migre senhas antes de alterar.",false);
   add("com.samsung.android.samsungpassautofill","Preenchimento Samsung Pass","Preenchimento de senhas Samsung; logins automáticos podem parar.",false);
   add("com.samsung.android.spayfw","Samsung Pay Framework","Integração de pagamentos Samsung; pagamento e serviços associados podem parar.",false);
   add("com.sec.android.daemonapp","Samsung Weather","Previsão do tempo e widgets; perde atualizações de clima.",true);
   add("com.samsung.android.dynamiclock","Tela de bloqueio dinâmica","Download e rotação de papéis de parede da tela de bloqueio.",true);
   add("com.samsung.android.app.dressroom","Papel de parede / estilo","Personalização de papel de parede Samsung; afeta opções de personalização.",false);
   add("com.samsung.android.themestore","Galaxy Themes","Loja de temas Samsung; temas instalados podem depender de serviços relacionados.",false);
   add("com.samsung.android.widget.pictureframe","Widget de fotos","Apresentação de fotos na tela inicial.",true);
   add("com.samsung.android.app.watchmanagerstub","Conexão Galaxy Watch","Auxiliar para configuração de relógios Galaxy; integração com relógios pode parar.",false);
   add("com.sec.android.usermanual","Manual do usuário","Acesso ao manual e ajuda do aparelho.",true);
   // Conservative profile is an explicit subset, not a conversion of all removals to disables.
   foreach(string id in new[]{"com.facebook.appmanager","com.facebook.services","com.facebook.system","com.samsung.android.app.spage","com.samsung.android.dynamiclock","com.sec.android.usermanual"})d[id].Safe="DESATIVAR";
   foreach(string id in new[]{"com.android.vending","com.android.chrome","com.samsung.android.samsungpass","com.samsung.android.samsungpassautofill","com.samsung.android.scloud","com.microsoft.skydrive","com.samsung.android.spayfw"})d[id].Risk="Alto";
   // Only entries actually present in the source inventory are offered at runtime.
   return d;
  }
  public static Info Get(string id) {
   Info value;
   if(Optional.TryGetValue(id,out value)) return new Info {Name=value.Name,Description=value.Description,Risk=value.Risk,Locked=false,Removable=value.Removable,Extreme=value.Extreme,Safe=value.Safe,Reason=value.Reason,Vendor=Vendor(id)};
   string name="Componente não documentado", reason="Função e dependências não confirmadas; bloqueado por precaução.", risk="Não avaliado";
   if(id=="com.osp.app.signin") {name="Samsung Account";reason="Identificado pelo APK SamsungAccount no inventário. Conta Samsung e integração de login com serviços Samsung; preservar autenticação e sincronização.";risk="Alto";}
   else if(id=="org.fdroid.fdroid" || id=="com.aurora.store" || id=="dev.imranr.obtainium.fdroid" || id=="com.uptodown") {name="Loja alternativa / atualização de aplicativos";reason="Preservado para instalar e atualizar aplicativos.";risk="Protegido";}
   else if(id=="com.google.android.gms" || id=="com.google.android.gsf") {name="Serviços fundamentais do Google";reason="Preserva autenticação, APIs Google e compatibilidade de jogos.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"qualcomm|^com\.qti\.|^vendor\.|gamedriver|gpudriver|vklayer|gpuwatch",RegexOptions.IgnoreCase)){name="Qualcomm / gráficos / hardware";reason="Preserva drivers e integrações de hardware, gráficos e jogos.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"dex|desktop|smartmirroring|inputshare|mcfserver|mcfds|audiomirroring|allshare|mdx",RegexOptions.IgnoreCase)){name="DeX / compartilhamento entre dispositivos";reason="Possível dependência de DeX, espelhamento, entrada ou integração entre dispositivos; preservar.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"\.game\.|sdhms|smartfps|\.lool$",RegexOptions.IgnoreCase)){name="Jogos / desempenho / controle térmico";reason="Preserva GOS, recursos de jogos e gerenciamento do aparelho.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"camera|gallery|photo|sticker|face|vision|filterprovider|ocr|singletake|sume",RegexOptions.IgnoreCase)){name="Câmera / imagem";reason="Possível dependência dos modos de câmera e processamento de imagens; preservar.";risk="Alto";}
   else if(Regex.IsMatch(id,@"wifi|wlan|bluetooth|usb|mtp|network|connectivity|tether|captiveportal|epdg|unifiedwfc",RegexOptions.IgnoreCase)){name="Conectividade";reason="Preserva Wi-Fi, Bluetooth, USB e redes.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"phone|telecom|telephon|dialer|incall|ims|ril|modem|carrier|cellbroadcast|messaging|\.rcs|\.stk|\.sim",RegexOptions.IgnoreCase)){name="Telefonia / mensagens / operadora";reason="Preserva chamadas, SIM, SMS e serviços da operadora.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"audio|sound|hearing|earphone|multisound|\.SMT$|\.tts$",RegexOptions.IgnoreCase)){name="Áudio / voz";reason="Preserva reprodução, roteamento de áudio e síntese de voz.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"storage|documentsui|myfiles|zarchiver|providers\.media|downloads",RegexOptions.IgnoreCase)){name="Arquivos / armazenamento";reason="Preserva acesso a arquivos, seleção de ROMs e armazenamento compartilhado.";risk="Crítico";}
   else if(Regex.IsMatch(id,@"installer|permissioncontroller|webview|keychain|security|knox|credential|auth|biometric",RegexOptions.IgnoreCase)){name="Instalação / segurança / runtime";reason="Preserva permissões, autenticação, instalação e componentes usados por outros apps.";risk="Crítico";}
   else if(id=="android" || Regex.IsMatch(id,@"^android\.|^com\.android\.|systemui|launcher|honeyboard|provider|overlay|\.settings|\.shell",RegexOptions.IgnoreCase)){name="Android / One UI / recursos do sistema";reason="Componente do sistema ou recurso compartilhado; preservar dependências do firmware.";risk="Alto";}
   else if(id=="com.openai.chatgpt" || id=="flar2.devcheck") {name="Aplicativo instalado pelo usuário";reason="Fora do escopo de bloatware solicitado; preservar.";risk="Protegido";}
   return new Info {Name=name,Description=reason,Risk=risk,Reason=reason,Vendor=Vendor(id)};
  }
  public static string Vendor(string id) {return id.StartsWith("com.google.")||id=="com.android.vending"||id=="com.android.chrome"?"Google":id.StartsWith("com.facebook.")?"Meta":id.StartsWith("com.microsoft.")?"Microsoft":Regex.IsMatch(id,@"^com\.(samsung|sec|osp|knox)\.")?"Samsung":id=="android"||id.StartsWith("android.")||id.StartsWith("com.android.")||Regex.IsMatch(id,@"qualcomm|^com\.qti\.|^vendor\.")?"System":"Outros";}
  public static string ProfileNote(string profile) {
   if(profile=="Gaming Seguro")return "Seguro: preserva loja, navegador, senhas, backups e apps cotidianos. Propõe desativar apenas auxiliares Meta, Samsung Free, tela dinâmica e manual; revise se usa essas funções.";
   if(profile=="Personalizado")return "Personalizado: começa em MANTER; você escolhe as ações nos aplicativos liberados. MANTER preserva o estado atual, inclusive se já estiver desativado.";
   return "Extremo: propõe mais alterações, incluindo Play Store, navegador e apps cotidianos. Confira o impacto antes de executar; pacotes protegidos continuam bloqueados.";
  }
 }
}
