using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SamsungDebloater {
 public sealed class CleanupDialog : Form {
  readonly ListView apps=new ListView();readonly TextBox detail=new TextBox();readonly Button review=new Button();
  public List<string> SelectedIds {get{return apps.CheckedItems.Cast<ListViewItem>().Select(x=>((Package)x.Tag).Id).ToList();}}
  public CleanupDialog(List<Package> candidates){
   Text="Limpeza opcional — todos os aplicativos não protegidos";Size=new Size(1160,680);MinimumSize=new Size(900,480);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9);
   var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(12),RowCount=4,ColumnCount=1};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,90));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));Controls.Add(layout);
   layout.Controls.Add(new Label {Dock=DockStyle.Fill,Text=candidates.Count+" aplicativos não protegidos instalados. Escolha apenas os que não usa; nada vem marcado.\nA coluna Ação indica REMOVER ou DESATIVAR quando a remoção não é permitida.\nPacotes bloqueados por função essencial ou identificação insuficiente continuam protegidos e não aparecem."},0,0);
   apps.Dock=DockStyle.Fill;apps.View=View.Details;apps.CheckBoxes=true;apps.FullRowSelect=true;apps.HideSelection=false;apps.MultiSelect=false;apps.Columns.Add("Aplicativo",200);apps.Columns.Add("Pacote",365);apps.Columns.Add("Estado atual",110);apps.Columns.Add("Ação",110);apps.Columns.Add("Risco / impacto",130);
   foreach(var p in candidates){var item=new ListViewItem(p.Info.Name){Tag=p,Checked=false};item.SubItems.Add(p.Id);item.SubItems.Add(p.State);item.SubItems.Add(Engine.CleanupAction(p));item.SubItems.Add(p.Info.Risk);apps.Items.Add(item);}
   apps.ItemChecked+=(s,e)=>{review.Enabled=apps.CheckedItems.Count>0;review.Text="Revisar ações ("+apps.CheckedItems.Count+")";};
   apps.SelectedIndexChanged+=(s,e)=>{if(apps.SelectedItems.Count>0){var p=(Package)apps.SelectedItems[0].Tag;detail.Text=p.Info.Name+" — "+Engine.CleanupAction(p)+"\r\n"+p.Info.Description+"\r\n"+(Engine.CleanupAction(p)=="DESATIVAR"?"Permite somente desativação para preservar a recuperação. Se já estiver desativado, nenhuma ação será necessária.":"Remove do usuário 0; o APK de fábrica permanece no sistema.")+" Use Restaurar com o snapshot para desfazer.";}};
   layout.Controls.Add(apps,0,1);detail.Multiline=true;detail.ReadOnly=true;detail.ScrollBars=ScrollBars.Vertical;detail.Dock=DockStyle.Fill;detail.Text="Marque um aplicativo para incluí-lo; selecione a linha para ler o impacto. Você ainda poderá cancelar na próxima tela.";layout.Controls.Add(detail,0,2);
   var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var skip=new Button {Text="Agora não",DialogResult=DialogResult.Cancel,AutoSize=true,Height=32};review.Text="Revisar ações (0)";review.AutoSize=true;review.Height=32;review.Enabled=false;review.DialogResult=DialogResult.OK;buttons.Controls.Add(skip);buttons.Controls.Add(review);layout.Controls.Add(buttons,0,3);CancelButton=skip;
  }
  public void VerifyAndCapture(string path){if(SelectedIds.Count!=0||review.Enabled)throw new Exception("Limpeza não pode começar com seleção.");if(apps.Items.Count==0)throw new Exception("Sem candidatos na fixture.");ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);Show();Application.DoEvents();apps.Items[0].Checked=true;if(SelectedIds.Count!=1||!review.Enabled)throw new Exception("Seleção da limpeza falhou.");apps.Items[0].Checked=false;if(SelectedIds.Count!=0||review.Enabled)throw new Exception("Desmarcar limpeza falhou.");Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}Hide();}
 }
}
