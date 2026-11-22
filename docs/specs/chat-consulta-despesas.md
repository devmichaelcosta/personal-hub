# Assistente de despesas por chat

> Status: concluído e validado em 21/11/2026. Build da solução: 0 avisos e 0 erros. Suíte completa: 43/43 testes aprovados, sem ignorados, incluindo 9 testes de integração SQL com bancos temporários. No navegador interno do Orca foram validados consulta de total mensal e rateio, despesas em aberto, extração de três lançamentos de uma imagem com revisão sem gravação, histórico entre páginas, temas claro/escuro, POST de chat HTTP 200 e console sem erros.

## Objetivo

Disponibilizar um chat no sistema para consultar despesas, gerar relatórios e cadastrar despesas por texto ou por imagens de recibos e extratos, usando IA local e preservando o histórico entre páginas e sessões.

## Requisitos trazidos pelo usuário

- O componente de chat deve estar disponível nas páginas do sistema.
- A pessoa poderá perguntar sobre despesas do mês, totais e relatórios.
- A pessoa poderá descrever uma despesa por texto ou enviar uma imagem de conta/recibo de mercado ou extrato bancário para identificar e cadastrar despesas.
- A conversa deverá continuar disponível em outras páginas e sessões, com histórico salvo no banco.
- Será mantida uma conversa contínua por usuário autenticado; limpar o histórico apaga as mensagens e os rascunhos dessa conversa.
- O histórico guardará texto e dados extraídos, mas não os arquivos de imagem; a imagem será usada em memória durante a extração e descartada ao fim do processamento.
- O sistema mostrará rascunhos editáveis e exigirá confirmação explícita antes de gravar cada despesa.
- Para mensagens com várias despesas, oferecerá confirmação em lote explícita. A operação em lote será atômica: todos os itens pendentes válidos são gravados juntos, ou nenhum é gravado.
- O chat permitirá localizar e editar uma despesa já cadastrada, apresentando uma proposta revisável e exigindo confirmação antes de aplicar a alteração.
- A edição pelo chat seguirá os campos, validações e comportamento do fluxo de edição completa do dashboard (`EdicaoDespesaViewModel` e `Despesa.Atualizar`). O chat não atualizará diretamente dados fora desse fluxo.
- O chat edita a despesa inteira pelo fluxo principal do dashboard; a edição isolada de uma parcela ou de uma ocorrência mensal não faz parte deste pedido.
- A aplicação roda na rede interna da casa, na máquina do usuário, e a IA deve usar Ollama local para evitar cobrança por chamadas de API.
- Em despesas compartilhadas consultadas, a resposta deve mostrar o total, a parte do usuário e as partes dos demais envolvidos naquela despesa.

## Contexto atual do sistema

- A aplicação principal é ASP.NET Core MVC/Razor em .NET 10, com Identity e SQL Server.
- As páginas usam `Views/Shared/_Layout.cshtml`; o widget pode ser renderizado ali para as páginas autenticadas.
- O cadastro atual usa título, descrição, valor por parcela, vencimento, quantidade de parcelas, tags, usuários envolvidos e tipo de cadastro. Há despesas parceladas e recorrentes.
- O dashboard calcula despesas a partir das parcelas e ocorrências recorrentes por vencimento. O modelo atual divide igualmente o valor de uma parcela/ocorrência entre os usuários associados.
- A página de relatórios soma o valor integral de parcelas/ocorrências. O chat deverá deixar claras as visões do valor integral e das partes individuais.
- O modelo local `qwen3.5:4b` foi instalado e testado com imagem. No teste, leu texto, mas acrescentou contexto que não estava visível e exibiu conteúdo de `Thinking`; as respostas precisam ser validadas e o raciocínio interno não deve aparecer como resposta final.

## Escopo completo da feature

### Chat em todas as páginas

- Widget flutuante no layout compartilhado, disponível para usuários autenticados.
- Envio de mensagens em português e anexação de imagens de recibos/contas e extratos bancários, descartadas após o processamento.
- Histórico persistido no SQL Server e associado ao usuário autenticado, disponível após navegação e nova sessão.
- Estado de carregamento, mensagens de erro recuperáveis e forma de limpar o histórico.

### Consultas e relatórios de despesas

- Consultar totais por mês e por período, despesas pagas, pendentes e vencidas.
- Listar despesas por período, texto, tag e situação.
- Agrupar despesas por tag e comparar períodos, incluindo meses.
- Materializar ocorrências recorrentes dentro do período consultado, inclusive vencimentos futuros no mesmo intervalo; limitar cada período a dez anos para evitar geração excessiva de linhas.
- Responder perguntas de acompanhamento usando o contexto recente da conversa e recalcular os resultados a partir do banco.
- Para despesas compartilhadas, apresentar valor integral e a divisão igual entre o usuário atual e os demais participantes da mesma despesa. Não revelar outras despesas pessoais desses participantes.
- Apresentar respostas textuais e tabelas/resumos que permitam conferir os itens que formaram o total.

### Cadastro de despesas por texto e imagem

- Interpretar mensagens como “registre uma compra de mercado de R$ 180 hoje” e preparar o lançamento com os dados disponíveis.
- Interpretar imagem de recibo/conta e extrair estabelecimento/título, data, valor total e, quando legíveis, itens ou descrição.
- Interpretar imagem de extrato bancário e identificar uma ou mais transações de saída como candidatas a despesas; transações ambíguas devem ser esclarecidas.
- Preparar os campos que o cadastro existente exige: título, descrição, valor por parcela, vencimento, quantidade de parcelas, tags, participantes e tipo de despesa. O usuário poderá revisar e editar esses campos; campos necessários que faltarem impedirão a confirmação.
- Validar os candidatos usando as regras e validações já existentes antes de persistir.
- Detectar possíveis duplicidades para evitar cadastrar novamente uma transação já registrada.

## Regras de dados e segurança

- O servidor obtém a identidade exclusivamente da autenticação do ASP.NET Identity. Nunca aceitar do texto, do arquivo ou da IA um `UserId` que amplie o acesso.
- Consulta e histórico são limitados ao usuário autenticado. Um usuário só pode ver a divisão dos participantes em uma despesa à qual ele próprio esteja vinculado.
- O cálculo de totais, parcelas, recorrências, status e divisão compartilhada é feito pelo backend. A IA interpreta a intenção e extrai campos; não calcula valores financeiros finais.
- A IA retorna uma estrutura tipada permitida, como `ConsultaPorPeriodo`, `TotalPorTag` ou `RascunhoDespesa`. Não gerar nem executar SQL livre, comandos ou código vindos do modelo.
- Mensagens, imagens e respostas são dados não confiáveis: validar tipo/tamanho do arquivo e conteúdo dos campos, renderizar texto com codificação segura e não executar conteúdo de documentos.
- O histórico não deve persistir nem exibir raciocínio interno `Thinking`; guardar a pergunta do usuário, a resposta final e os metadados necessários ao fluxo. Não guardar o binário da imagem original.
- Não registrar conteúdo de extratos, recibos ou mensagens financeiras em logs técnicos.
- Usar somente logging nativo e não registrar conteúdo das rotas do chat em serviços externos.
- O navegador chama somente o MVC. O MVC chama Ollama em `127.0.0.1:11434`; não expor essa porta na rede doméstica nem na internet.
- O acesso externo do sistema continua protegido por autenticação. A inferência e o processamento de documentos permanecem locais, sem API de IA paga.

## Arquitetura proposta

- **Interface MVC/Razor:** partial `Views/Shared/_ChatWidget.cshtml` incluída no `_Layout.cshtml`, JavaScript para mensagens e anexos e CSS seguindo os temas claro/escuro existentes.
- **Endpoint MVC autenticado:** recebe mensagem/anexo, carrega o histórico do usuário, encaminha ao caso de uso e retorna respostas e rascunhos; aplicar antiforgery nos POSTs.
- **Camada de Aplicação:** casos de uso separados para consultar, gerar relatório e preparar/cadastrar despesas; contratos estruturados e validação de intenção/campos.
- **Infraestrutura de IA:** cliente HTTP para Ollama local configurado no servidor, com timeout, cancelamento e tratamento de indisponibilidade; sem chamada direta do JavaScript ao Ollama.
- **Persistência:** entidades de conversa e mensagens vinculadas ao `Usuario`, migration do EF Core e isolamento do histórico por proprietário. Apenas texto e dados estruturados permanecem no banco; arquivos de imagem não são persistidos.
- **Consultas:** reutilizar as regras de parcelas, ocorrências, tags, pagamentos e participantes; evitar duplicar cálculos em controller, JavaScript ou resposta do modelo.
- **Cadastro:** converter a intenção ou imagem em rascunho compatível com `CadastroDespesaViewModel`; após as validações e a regra de confirmação definida pelo usuário, persistir pelo fluxo de aplicação/domínio.
- **Relatórios:** construir agregações determinísticas na aplicação e apresentar totais, partes e itens de origem; o modelo não deve inventar ou recalcular números.

## Fluxos principais

### Consulta

1. O usuário pergunta, por exemplo, “Quanto gastei com mercado este mês?”
2. O servidor identifica o usuário autenticado e manda ao modelo local apenas o contexto necessário para produzir uma intenção estruturada.
3. O caso de uso valida período, tag, situação e tipo de consulta.
4. Se a pergunta for ambígua, o chat pede esclarecimento; caso contrário, a aplicação consulta parcelas e ocorrências autorizadas e calcula os valores.
5. A resposta mostra os totais verificáveis e, para despesas compartilhadas, o total e as partes dos participantes envolvidos.
6. Pergunta e resposta final são associadas ao histórico do usuário.

### Cadastro por texto ou documento

1. O usuário descreve uma compra ou anexa um recibo/extrato.
2. O modelo local extrai um ou mais candidatos estruturados; não cria registros diretamente.
3. O sistema sinaliza campos ilegíveis, possíveis duplicidades, tags sem correspondência e dados necessários que estejam faltando.
4. O usuário revisa e pode editar cada rascunho, preenchendo os campos necessários.
5. Somente após confirmação explícita o backend valida e cadastra cada despesa pelo domínio e pelos casos de uso do sistema, depois informa o resultado no chat.
6. A solicitação em texto, os dados extraídos e o resultado ficam no histórico do usuário; o arquivo da imagem é descartado após a inferência.

### Edição de despesa existente pelo chat

1. O usuário pede a edição informando texto suficiente para identificar a despesa e os campos que deseja alterar.
2. O servidor procura apenas despesas associadas ao usuário autenticado. Nenhum identificador de despesa fornecido pelo modelo é aceito como autorização.
3. Se não houver correspondência ou houver mais de uma candidata, o chat pede uma descrição mais específica e não cria uma proposta de alteração.
4. Para uma única correspondência, o chat carrega os campos atuais pelo mesmo `EdicaoDespesaViewModel` do dashboard, aplica somente as alterações solicitadas e apresenta o formulário editável.
5. A alteração só é persistida após confirmação explícita. O servidor valida novamente o proprietário, os campos e o estado atual, chama `Despesa.Atualizar` e salva em transação.
6. Se a despesa tiver sido alterada desde a proposta, o chat rejeita a confirmação e pede que o usuário gere uma proposta atualizada.

## Critérios de aceite

- **Dado** um usuário autenticado, **quando** abrir qualquer página autenticada, **então** o chat estará disponível e ligado ao histórico desse usuário.
- **Dada** uma pergunta sobre um mês, **quando** houver parcelas ou recorrências no período, **então** os valores serão calculados por vencimento sem somar o valor total da despesa no mês errado.
- **Dada** uma consulta por despesas “em aberto”, **quando** houver parcelas não pagas no período, **então** serão incluídas tanto as ainda não vencidas quanto as vencidas; “pendentes” incluirá apenas as que ainda não venceram, e “vencidas” apenas as que passaram do vencimento.
- **Dada** uma recorrência com vencimento futuro dentro do período consultado, **quando** o chat montar o relatório, **então** a ocorrência prevista também será incluída sem depender de abrir o dashboard.
- **Dada** uma despesa compartilhada vinculada ao usuário, **quando** a consulta incluir essa despesa, **então** a resposta mostrará o total e a parte de cada participante; despesas pessoais não relacionadas dos demais usuários não serão incluídas.
- **Dado** um pedido de relatório por tag, situação ou comparação de meses, **quando** os filtros forem válidos, **então** a resposta exibirá agregações calculadas pelo backend e os itens que as compõem.
- **Dada** uma mensagem de cadastro por texto, **quando** ela contiver campos suficientes, **então** o sistema preparará os campos do cadastro e aplicará as validações já existentes antes de persistir.
- **Dada** uma imagem de recibo legível, **quando** for processada pelo Ollama local, **então** o sistema identificará os dados extraídos, descartará o arquivo e permitirá editar e confirmar o rascunho antes de gravar.
- **Dado** um valor de moeda extraído com precisão maior que centavos, **quando** o rascunho for preparado, **então** o valor exibido será arredondado para duas casas decimais.
- **Dada** uma imagem de extrato com várias transações, **quando** for processada, **então** cada transação candidata será apresentada separadamente e itens ambíguos/possivelmente duplicados não serão cadastrados sem resolução.
- **Dada** uma transação candidata com mesmo valor e data de uma parcela ou ocorrência recorrente já registrada para algum participante, **quando** o usuário confirmar, **então** o sistema sinalizará a possível duplicidade antes de gravar.
- **Dada** uma transação de extrato com data e descrição textual, **quando** o rascunho for preparado, **então** a data será atribuída ao vencimento e o estabelecimento/beneficiário será priorizado no título; uma data não será usada como título.
- **Dado** um extrato com várias despesas pendentes, **quando** o usuário selecionar “Confirmar todas”, **então** todas as despesas válidas serão gravadas em uma única transação e todas serão marcadas como confirmadas.
- **Dado** um lote em que algum lançamento seja inválido, possivelmente duplicado sem confirmação, ou tenha sido confirmado em outra ação, **quando** o usuário tentar confirmar todas, **então** nenhum lançamento do lote será gravado e o chat indicará os itens que precisam ser resolvidos.
- **Dado** um pedido para editar uma despesa associada ao usuário autenticado, **quando** o chat encontrar uma única correspondência, **então** mostrará os valores atuais e as alterações propostas em campos editáveis e aguardará confirmação explícita.
- **Dada** uma instrução que identifica a despesa pelo título atual, inclusive em frases como “altere título da despesa X para Y”, **quando** o chat interpretar a mensagem, **então** localizará a despesa por X e preparará Y como novo título sem gravar até confirmação.
- **Dado** que o usuário confirme uma edição, **quando** o servidor persistir a alteração, **então** aplicará as validações do `EdicaoDespesaViewModel` e o comportamento de `Despesa.Atualizar`, como na edição completa do dashboard.
- **Dado** um pedido de edição com nenhuma ou múltiplas correspondências, **quando** o chat procurar a despesa, **então** pedirá esclarecimento sem alterar registros.
- **Dado** que a despesa, uma parcela ou uma ocorrência recorrente tenha sido alterada depois da criação da proposta, **quando** o usuário confirmar a proposta antiga, **então** a alteração será recusada para evitar confirmar dados revistos com estado desatualizado.
- **Dado** que faltem valor, data, tag ou participantes necessários, **quando** a IA não puder identificá-los com segurança, **então** o chat perguntará ao usuário em vez de inventar os dados.
- **Dada** uma resposta inválida do modelo, instrução maliciosa embutida em imagem ou tentativa de SQL/código, **quando** o servidor validar a resposta, **então** a consulta ou gravação será rejeitada e nenhum dado não autorizado será exposto ou alterado.
- **Dada** uma conversa salva, **quando** o usuário navegar ou iniciar nova sessão, **então** seu histórico estará disponível e isolado dos outros usuários; a ação de limpar apagará apenas o histórico próprio.
- **Dado** que o Ollama esteja indisponível ou exceda o timeout, **quando** uma consulta ou cadastro for enviado, **então** o chat explicará a indisponibilidade e não gravará um rascunho incompleto como despesa.
- **Dada** uma resposta que contenha `Thinking`, **quando** o chat a apresentar e salvar, **então** somente a resposta final estará visível e persistida.

## Comportamentos padrão incorporados ao fluxo de revisão

Os dados continuam editáveis pelo usuário antes da confirmação explícita:

1. **Recibo:** preparar uma despesa pelo valor total, sem transformar cada item em lançamento separado.
2. **Participantes:** selecionar o usuário atual por padrão e incluir pessoas indicadas apenas quando houver correspondência única; nomes ambíguos ou desconhecidos geram um aviso visível.
3. **Tags:** reutilizar somente tags existentes; tags não reconhecidas geram aviso e podem ser substituídas ou removidas no rascunho.
4. **Pagamento:** considerar paga uma transação bancária claramente concluída; o campo permanece editável antes de gravar.
5. **Relatórios:** apresentar resumo e tabelas; agrupar por tag e comparar períodos, mostrando total integral e parte do usuário.

## Plano de construção do escopo completo

1. Concluído: manter a especificação completa como contrato de implementação, com revisão editável antes da gravação.
2. Concluído: persistir o histórico sem guardar os arquivos enviados.
3. Concluído: integrar o Ollama local com extração estruturada e validação para consultas, relatórios e cadastros.
4. Concluído: calcular despesas, partes compartilhadas, status, tags e relatórios; verificar possíveis duplicidades contra despesas, parcelas e ocorrências recorrentes.
5. Concluído: disponibilizar endpoint autenticado e widget global com revisão, confirmação e mensagens de erro.
6. Concluído: cobrir parsing, rateio e assinaturas contra alterações de parcelas/ocorrências com testes, compilar a solução e validar o tema claro/escuro na aplicação local.
7. Concluído: confirmar lançamentos individuais ou em lote com validação integral e transação atômica, incluindo proteção contra confirmações concorrentes do chat.
8. Concluído: editar a despesa inteira pelo chat, com formulário revisável, confirmação explícita, validação equivalente ao fluxo completo do dashboard e recusa de proposta desatualizada.

> Os itens acima são frentes para construir a feature solicitada inteira; não representam versões nem cortes de escopo.

## Evidências da validação final (21/11/2026)

- **Build:** `dotnet build PersonalHub.sln --no-restore -m:1` concluiu com 0 avisos e 0 erros.
- **Suíte automatizada:** `dotnet test PersonalHub.sln --no-restore -m:1`: 43 aprovados, 0 falhas e 0 ignorados. O comando preencheu temporariamente `CHAT_INTEGRATION_TEST_CONNECTION` a partir de User Secrets; os 9 testes de integração criaram bancos SQL Server isolados e os removeram ao terminar.
- **Unidade e integração:** a frase original `Preciso que me informe o total de gastos do mes atual.` passou pelo parser, consultou o banco e conferiu total e rateio; também foram exercitados isolamento por usuário, relatório por tag e comparação, recorrência futura, despesas em aberto incluindo vencidas não pagas, confirmação de lote válida/inválida, privacidade do histórico e indisponibilidade do Ollama sem gravar despesas.
- **Navegador Orca:** em `https://localhost:7190`, a consulta do mês atual retornou 9 lançamentos, total integral de R$ 1.778,52 e parte do usuário de R$ 1.763,07, em acordo com o dashboard. A consulta de despesas em aberto do mês anterior incluiu o item vencido ainda não pago. Uma imagem sintética de extrato foi extraída em três rascunhos editáveis com título, data e valor separados; os rascunhos desta verificação permaneceram sem confirmação e não geraram lançamentos.
- **Interface e transporte:** o widget permaneceu disponível ao navegar do dashboard para despesas e o histórico foi mantido; temas claro e escuro foram inspecionados. O POST `/Chat/Enviar` respondeu HTTP 200 e o console do navegador não registrou erros.

O ambiente de teste SQL e a conta usada no navegador são de desenvolvimento. Nenhuma imagem enviada foi guardada pelo chat.
