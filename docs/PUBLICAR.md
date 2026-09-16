# Publicar no GitHub

Use somente o conteúdo do ZIP `Samsung-App-Debloater-source.zip` ou a pasta limpa
`Samsung-App-Debloater`. Não envie a pasta de trabalho original inteira: ela contém seus
backups, inventários e logs locais. `.gitignore` protege operações Git normais,
mas não impede upload manual ou `git add -f`.

1. Crie um repositório vazio no GitHub, por exemplo `samsung-app-debloater`, sem gerar outro README ou licença.
2. Extraia o ZIP e abra a pasta dos fontes no GitHub Desktop.
3. Crie um repositório local nessa pasta, confira os arquivos incluídos e faça o primeiro commit.
4. Use **Publish repository** e escolha a visibilidade desejada.

Alternativa: envie os arquivos pelo site do GitHub, preservando `.github/workflows`,
`.gitignore` e `.gitattributes`. Envie o conteúdo extraído, não apenas o ZIP.

A licença escolhida para o código é MIT. Não há conta ou endereço de repositório
preenchidos automaticamente. Nenhum commit remoto ou publicação foi feito pelo programa.

Para futuras versões, `export-source.ps1` gera uma nova pasta com apenas os arquivos
de código e documentação explicitamente permitidos. Pastas existentes não são apagadas.
Confira a lista mostrada pelo script antes de publicar.
