# Contribuir

Use Windows e execute `powershell -ExecutionPolicy Bypass -File .\build.ps1`.
Execute `powershell -ExecutionPolicy Bypass -File .\test.ps1` para testes com
ADB em memória, sem acesso a um aparelho. `-NoUI` desativa a renderização da interface.

Para mudar a política de pacotes, inclua identificação, fontes e impacto da função
removida, com testes das proteções. Não libere pacotes apenas pelo fabricante ou
pela semelhança do nome. Mantenha confirmação, snapshots anteriores à operação e
restauração do estado original. Não introduza comandos destrutivos nos testes.

Ao abrir uma issue ou pull request, descreva o problema, o resultado esperado,
modelo/Android e validação realizada. Não anexe dumps, backups, logs completos,
seriais, contas, tokens ou dados pessoais. Use exemplos fictícios e trechos revisados.

Contribuições de código ao projeto são disponibilizadas sob a licença MIT do repositório.
