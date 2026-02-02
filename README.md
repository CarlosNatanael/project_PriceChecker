# Verificador de Preço (PriceChecker)

O **Verificador de Preço** é um Serviço Windows desenvolvido em C# (.NET Framework 4.7.2) projetado para atuar como um servidor central para terminais de consulta de preços em estabelecimentos comerciais. 

O sistema comunica com os terminais através da biblioteca nativa `VP_v3.dll`, gerindo conexões via TCP/IP, processando solicitações de códigos de barras e devolvendo informações de produtos (nome e preço) para o display do terminal.

##  Funcionalidades

- **Execução em Segundo Plano:** Como Serviço Windows, o sistema inicia automaticamente com o sistema operativo, sem necessidade de login de utilizador.
- **Gestão de Terminais:** Identifica automaticamente novos terminais que se conectam à rede e monitoriza desconexões.
- **Processamento de Consultas:** Recebe códigos de barras capturados pelos terminais e envia a resposta correspondente (preço do produto ou mensagem de "não encontrado").
- **Sistema de Log:** Registo detalhado de todas as operações e erros em ficheiros de texto para facilitar a manutenção e auditoria.
- **Interoperabilidade Nativa:** Integração robusta com bibliotecas C++ através de P/Invoke.

##  Tecnologias Utilizadas

- **Linguagem:** C#
- **Framework:** .NET Framework 4.7.2
- **Tipo de Projeto:** Windows Service
- **Biblioteca de Hardware:** `VP_v3.dll` (comunicação com terminais)

##  Estrutura do Projeto

- `Service1.cs`: Lógica principal do serviço, timers de verificação e integração com a DLL.
- `ProjectInstaller.cs`: Componentes necessários para a instalação do serviço no Windows.
- `Program.cs`: Ponto de entrada que inicializa o serviço.
- `Logs/`: Pasta gerada automaticamente contendo os registos de execução.

##  Instalação

Para instalar este serviço no Windows, utiliza o utilitário `installutil.exe` do .NET Framework:

1. Abre a Linha de Comandos (CMD) como Administrador.
2. Navega até à pasta do executável compilado (`bin\Debug` ou `bin\Release`).
3. Executa o comando:
```bash
installutil.exe VerificadorPreco.exe
```

4. Abre o gestor de "Serviços" do Windows (services.msc) e inicia o serviço **VerificadorDePreco.demo**.

##  Notas de Desenvolvimento

Atualmente, o projeto utiliza uma lógica de demonstração com dados fixos (hardcoded) para o produto "LIMÃO". Para produção, deve ser implementada a conexão a uma base de dados SQL no método `TmTerminal_Tick`.