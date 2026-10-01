# Changelog

## 0.1.2 — 2026-10-01

- Publica os avisos visuais, a sugestão opcional de relaxar, o adiamento de cinco minutos, a pausa no bloqueio e a inicialização opcional com o Windows.
- Reúne as melhorias das três prévias abaixo, com textos revisados e instruções de instalação atualizadas.
- 55 verificações de lógica e 59 verificações de interface e ciclo de vida.

## 0.1.2-preview.3 — 2026-10-01 (teste local)

- Inicialização com o Windows opcional, desativada por padrão e limitada à conta atual. Salvar com a opção desmarcada remove a entrada do app.
- Pausa ao bloquear a sessão, preservando o tempo restante, com escolha entre continuar ou manter pausado ao desbloquear. Pausas manuais e janelas de horário são respeitadas.
- Botão **Daqui a 5 minutos** nos avisos reais de postura. Mantém a postura anterior, repete o aviso cinco minutos depois e inicia a próxima etapa completa sem mudar os intervalos salvos.
- Clicar na seta persistente reabre as opções do aviso. As prévias não alteram a rotina.
- Revisão de textos, uso de aspas em nomes de botões/status e explicações mais completas das opções.

## 0.1.2-preview.2 — 2026-09-29 (teste local)

- Simplifica os status para **Trabalhando** e **Relaxando**. O antigo **Jogando** migra para **Relaxando**, sem perder as outras preferências.
- Sugestão opcional após uma pausa de 10 minutos sem interação, ajustável entre 2 e 60 minutos. Aparece ao voltar ao computador, apenas durante a rotina de trabalho.
- A consulta usa somente o timestamp de última interação da sessão do Windows, a cada 15 segundos. Sem captura de teclas, cliques, tela, aplicativos, histórico ou transmissão de dados.
- O status só muda ao clicar em **Relaxar**. Ignorar ou escolher **Continuar trabalhando** mantém o ciclo. A sugestão não tem som, não tira o foco, expira em 30 segundos e tem intervalo mínimo de 30 minutos.
- Opção de desativar na aba **Atividade**, interrompendo as consultas. Prioriza os avisos de postura e não sugere durante configurações, pausa, descanso ou fora do horário.
- 36 testes de rotina/preferências/inatividade e 41 verificações de interface/ciclo de vida.

## 0.1.2-preview.1 — 2026-09-29 (teste local)

- Balão próprio nas mudanças de postura, com duas pulsações suaves e sem ativar a janela.
- Duração padrão de 12 segundos, ajustável entre 5 e 30 segundos.
- Depois do balão, uma seta permanece até clicar no aviso ou no mascote.
- Aba **Avisos visuais** com ativação, duração e prévias silenciosas de levantar e sentar.
- Avisos acompanham o mascote, respeitam as bordas do monitor e são encerrados junto com o aplicativo.
- 21 verificações da rotina/preferências e 30 verificações de ciclo de vida e avisos no Windows.

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
