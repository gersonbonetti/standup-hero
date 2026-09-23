# Changelog

## 0.1.1 — 2026-09-23

- Impede múltiplas instâncias na mesma sessão do Windows, inclusive ao executar cópias em pastas diferentes. Abrir novamente exibe o mascote existente.
- O reset salva e aplica a rotina padrão imediatamente, substituindo qualquer prazo anterior sem precisar clicar em Salvar.
- Acrescenta **Encerrar aplicativo** nas configurações e explicita que **Ocultar** mantém os lembretes.
- Encerra timers, sons, janela de configurações e ícone da bandeja ao fechar o aplicativo.
- Adiciona 12 verificações de instância única, reset e encerramento.

Ao atualizar da v0.1.0, encerre todas as cópias antigas antes de abrir a nova versão.

## 0.1.0 — 2026-09-23

Primeira versão pública do StandUp Hero para Windows.

- Mascote pixel art transparente e arrastável, próximo à barra de tarefas.
- Personagens homem e mulher, com posturas sentado, em pé e espreguiçando.
- Ciclos de 45/15 minutos por padrão, com dias e horários configuráveis.
- Modos trabalhando, jogando e relaxando; pausa e troca manual de postura.
- Quatro sons incorporados, selecionados separadamente para levantar e sentar.
- Restauração da rotina padrão e preferências salvas localmente.
- Ícone próprio no executável, bandeja e configurações.
- Testes da rotina e build automatizado no GitHub Actions.

Distribuição inicial portátil para Windows x64, sem instalador ou assinatura digital.
