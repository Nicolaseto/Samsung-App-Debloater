# Samsung App Debloater

Aplicativo Windows em português para analisar e gerenciar aplicativos do usuário 0
em celulares Samsung com Android 12 a 17, via ADB e USB, sem root.

## Compatibilidade prevista na versão 2.0

Identifica fabricante, modelo, Android, API e codename pelo ADB. Aceita Samsung
com Android 12/12L (API 31/32), 13 (33), 14 (34), 15 (35), 16 (36) e 17 (37).
Fabricante desconhecido, metadados inconsistentes, versões anteriores/futuras
e codenames de prévia permitem somente análise, sem alterações.
A disponibilidade do Android depende do modelo e das atualizações da Samsung;
o programa não instala nem atualiza Android. Codename REL não comprova ausência de beta do fabricante.

**Compatibilidade prevista não significa validação física em todos os aparelhos.**
O inventário real inicial veio de um S20 FE com Android 13. As demais versões têm
testes de análise, execução e restauração com ADB simulado. Variações de firmware,
políticas corporativas e bloqueios do fabricante podem impedir comandos.
Dados incompletos bloqueiam execução; falhas interrompem o lote e ficam no snapshot.
O parser aceita campos userId e appId do Package Manager.

O perfil inicial é **Gaming Seguro**; **Samsung Gaming Extremo** propõe mais alterações.
Somente pacotes conhecidos presentes no aparelho são elegíveis; desconhecidos
continuam protegidos. Não foi adicionada uma lista genérica de remoção.
Snapshots antigos de formato 2 continuam legíveis, exigindo mesmo aparelho e firmware.

Referências: [Android 17](https://developer.android.com/about/versions/17) e
[API 37](https://developer.android.com/about/versions/17/setup-sdk), consultadas em 16/09/2026.

Projeto independente, sem vínculo com Samsung ou Google. **Não há garantia de ganho
de FPS ou de compatibilidade com todos os firmwares.** Os testes automatizados usam
ADB simulado; não validam o funcionamento de cada jogo ou acessório no celular real.

## Recursos

- Modelo, versão Android, conexão e inventário completo de pacotes.
- Perfis **Gaming Seguro**, **Samsung Gaming Extremo** e **Personalizado**.
- Ações **MANTER**, **DESATIVAR** e **REMOVER**, com pesquisa e filtros por fabricante.
- Descrições e riscos para aplicativos identificados; pacotes desconhecidos ficam bloqueados.
- Proteção de Android, One UI essencial, telefonia, conectividade, câmera, áudio,
  armazenamento, instalador, permissões, Qualcomm, gráficos, DeX, GOS, serviços Google
  e lojas alternativas. UIDs privilegiados/compartilhados podem impor bloqueios adicionais.
- Limpeza opcional após otimização ou pelo botão **Limpar aplicativos**: exibe todos
  os aplicativos não protegidos, com a ação permitida. Nada vem marcado.
- Confirmação de comandos, snapshots anteriores à execução, logs completos e
  restauração individual ou em lote. Netflix e YouTube aparecem quando elegíveis.

## Compilar no Windows

Requer .NET Framework 4.5 ou superior, com compilador `csc.exe` do Framework de 64 bits.
O Windows 10/11 normalmente possui .NET Framework 4.x; confirme a presença do compilador
em `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
Não requer Python, Node, SDK moderno do .NET ou pacotes NuGet.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Saída: `Samsung-App-Debloater.exe`. O parâmetro `-OutputFile` permite escolher outro nome.

Para usar um celular, baixe o [Platform Tools oficial para Windows](https://developer.android.com/tools/releases/platform-tools)
e extraia `platform-tools` ao lado do executável, incluindo suas DLLs e avisos de licença.
O aplicativo usa essa cópia local do ADB, sem depender do PATH. **ADB não é incluído no
repositório de fontes**; veja [avisos de terceiros](THIRD_PARTY_NOTICES.md).

## Usar

1. Abra o executável. Para conhecer a interface sem celular, use **Ver inventário salvo**:
   em uma instalação limpa aparece uma demonstração claramente identificada, com dados fictícios.
2. Conecte exatamente um aparelho USB, habilite Depuração USB e aceite a chave RSA no celular.
3. Clique em **Analisar celular**. A ação apenas consulta e salva dados localmente.
4. Escolha o perfil e revise as propostas. **Gaming Seguro** propõe somente seis desativações
   de auxiliares Meta, Samsung Free, tela dinâmica e manual, quando presentes e elegíveis.
   **Extremo** propõe mais alterações; **Personalizado** começa em MANTER.
5. Clique em **Otimizar** e confirme somente após ler a lista. Selecionar um perfil não executa nada.
6. Na limpeza opcional, marque os aplicativos desejados, clique em **Revisar ações** e confirme.
   **Agora não** cancela essa etapa. O botão **Limpar aplicativos** também permite acessá-la diretamente.
7. Para desfazer, use **Restaurar**, escolha o snapshot da operação e os aplicativos.
   Restaure lotes do mais recente para o mais antigo.

**MANTER** preserva o estado atual: não reativa um aplicativo já desativado.
A Play Store permite somente desativação: o perfil Seguro a mantém; o Extremo propõe
desativá-la, o que pode afetar licenças e atualizações. Aplicativos fora do sistema
permitem no máximo desativação. A política não libera pacotes apenas pelo fabricante.
Os totais exibidos variam conforme o inventário, firmware e alterações anteriores.

## Snapshots e limitações

Snapshot **não é backup de APKs nem de dados pessoais**. A remoção usa
`pm uninstall -k --user 0`; o APK de fábrica permanece no sistema e não há promessa
de recuperar esse espaço. A restauração usa `install-existing` quando necessário e
restaura o valor original de `enabled`. Depende de o pacote continuar no firmware.
Remover um aplicativo não cancela assinaturas.

Falhas interrompem o lote e deixam registros de etapas pendentes, concluídas ou incertas.
Mudanças no inventário, no firmware ou no aparelho bloqueiam ações incompatíveis.
Use snapshots de operação com sufixo `otimizacao.json` ou `restauracao.json`;
`analise.json` contém apenas a leitura. O formato atual de snapshot é 2.

## Testes sem aparelho

```powershell
powershell -ExecutionPolicy Bypass -File .\test.ps1
# Sem renderização da interface, como no CI:
powershell -ExecutionPolicy Bypass -File .\test.ps1 -NoUI
```

Os testes usam inventário sintético e transporte ADB em memória. Não precisam de ADB,
USB ou inventário pessoal. Cobrem parsing, perfis, proteções, seleção, falhas parciais,
desconexão, restauração e controles da interface. Resultados e imagens gerados são ignorados pelo Git.
O workflow `.github/workflows/build.yml` compila e testa no Windows do GitHub Actions.

O executável também aceita `--analyze-only` para consultar um aparelho real sem aplicar
alterações. Esta opção exige ADB e salva informações privadas localmente.

## Privacidade e publicação

`backups/`, `logs/`, `evidence/`, inventários, relatórios, binários e dados de testes
estão no `.gitignore`. Dumps podem conter identificadores do aparelho, aplicativos e
outras informações privadas. Não envie esses arquivos a issues nem use `git add -f` neles.
O projeto não envia telemetria nem publica dados automaticamente.

Use `export-source.ps1` para criar uma cópia com uma lista explícita de arquivos públicos.
O script não apaga pastas existentes. Consulte [como publicar no GitHub](docs/PUBLICAR.md).

## Código e contribuição

| Arquivo | Função |
| --- | --- |
| `Debloater.cs` | Interface Windows Forms e confirmação |
| `Cleanup.cs` | Seleção de aplicativos para limpeza |
| `Core.cs` | ADB, validações, transações e snapshots |
| `Catalog.cs` | Descrições e política de proteção |
| `DemoInventory.cs` | Inventário fictício público |
| `Tests.cs` | Simulador e testes |

Veja [CONTRIBUTING.md](CONTRIBUTING.md). Código do projeto sob a [licença MIT](LICENSE).
As identificações de pacotes são avaliações do projeto, não certificações do fabricante.

Referências: [documentação ADB](https://developer.android.com/tools/adb) e
[Package Manager do Android 13](https://android.googlesource.com/platform/frameworks/base/+/refs/heads/android13-release/services/core/java/com/android/server/pm/PackageManagerShellCommand.java).
