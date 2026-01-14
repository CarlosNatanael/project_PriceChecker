using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerificadorPreco
{
    public partial class Service1 : ServiceBase
    {
        // Timer principal para verificar novos terminais
        private Timer Timer;
        // Timer para tratar as solicitações dos terminais conectados
        private Timer TmTerminal;
        private uint IpTerminal = 0;

        // Função que Inicializa Serviço
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void vInitialize();

        // Função que Inicializa Servidor
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tc_startserver();

        // Função que Identifica Terminais Conectados ao Servidor
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool bConnected(out uint ID_IP, out ushort Porta);

        // Função que Identifica Terminais Desconectados do Servidor
        [DllImport("VP_v3.dll")]
        public static extern bool bDisconnected(out uint ID_IP, out ushort ID_Socket);

        // Função que Desconecta dado Terminal
        [DllImport("VP_v3.dll")]
        public static extern bool bCloseTerminal(uint ID_IP, ushort ID_Socket);

        // Formatação de Valores
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr Inet_NtoA(uint ID_IP);

        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint Inet_Addr(IntPtr sIP);

        // Função que busca a versão da DLL
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        static extern IntPtr dll_version();

        // Função para enviar mensagem para as linhas 1 e 2 do terminal
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool bSendDisplayMsg(uint ID_IP, IntPtr Linha1, IntPtr Linha2, int Tempo);

        // Função usada periodicamente para identificar se houve solicitação de pesquisa de código de barras pendente
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr bReceiveBarcode(out uint ID_IP, out ushort ID_Socket, out int Nbr);

        // Função usada para Informar a aplicação que o terminal solicitante recebeu a mensagem
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool bSendProdNotFound(uint ID_IP);

        // Envia para a primeira e segunda linha do display do terminal solicitante o nome e o preço do produto pesquisado
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool bSendProdPrice(UInt32 IP, IntPtr Prod, IntPtr Valor);

        // Função que Finaliza Serviço
        [DllImport("VP_v3.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern bool bTerminate();

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToLog("OnStart iniciado.");
            Servico();
            WriteToLog("OnStart concluído.");
        }

        private void Servico()
        {
            WriteToLog("Serviço sendo inicializado.");
            vInitialize();
            WriteToLog("Serviço Inicializado!");

            IntPtr dados = dll_version();
            string versao = Marshal.PtrToStringAnsi(dados);
            WriteToLog("Versão da DLL: " + versao);

            if (tc_startserver() == 1)
            {
                WriteToLog("Servidor iniciado com sucesso.");

                Timer = new Timer(3000);
                Timer.Elapsed += new ElapsedEventHandler(Timer_Tick);
                WriteToLog("Timer configurado para 3 segundos.");
                Timer.Enabled = true;
                WriteToLog("Timer habilitado.");
            }
            else
            {
                WriteToLog("Falha ao iniciar o servidor.");
            }
        }

        protected override void OnStop()
        {
            WriteToLog("OnStop iniciado.");
            Timer.Enabled = false;
            WriteToLog("Timer principal desabilitado.");

            TmTerminal.Enabled = false;
            WriteToLog("Timer de terminais desabilitado.");

            bTerminate();
            WriteToLog("Serviço finalizado.");
        }

        private void Timer_Tick(object sender, ElapsedEventArgs e)
        {
            WriteToLog("Timer principal disparado.");
            WriteToLog("Verificando novos terminais conectados...");

            if (bConnected(out uint Term, out ushort Porta))
            {
                string ipAddress = Marshal.PtrToStringAnsi(Inet_NtoA(Term));
                WriteToLog($"Novo Terminal {ipAddress} conectado na porta {Porta}");
                IpTerminal = Term;

                // Inicializa o Timer de Terminais apenas se houver uma nova conexão
                TmTerminal = new Timer(1000);
                TmTerminal.Elapsed += new ElapsedEventHandler(TmTerminal_Tick);
                WriteToLog("Timer de terminais configurado para 1 segundo.");
                TmTerminal.Enabled = true;
                WriteToLog("Timer de terminais habilitado.");

                Timer.Enabled = false;
                WriteToLog("Timer principal desabilitado.");
            }
            else
            {
                WriteToLog("Nenhum novo terminal conectado.");
            }
        }

        private static readonly object _LogLock = new object();

        private void TmTerminal_Tick(object sender, ElapsedEventArgs e)
        {
            WriteToLog("Timer de terminais disparado.");

            // Converte a string para ponteiro.        
            IntPtr Cod = Marshal.AllocHGlobal(64);
            IntPtr Prod = Marshal.AllocHGlobal(64);
            IntPtr Valor = Marshal.AllocHGlobal(64);

            try
            {
                string CodBarras = "7891027230036";
                string ProdDados = "LIMAO";
                string ValorDados = "2,00";
                Prod = Marshal.StringToHGlobalAnsi(ProdDados);
                Valor = Marshal.StringToHGlobalAnsi(ValorDados);

                Cod = bReceiveBarcode(out uint Term, out ushort Porta, out int Nbr);
                string CodBar = Marshal.PtrToStringAnsi(Cod);

                if (string.IsNullOrWhiteSpace(CodBarras))
                {
                    return;
                }

                try
                {
                    WriteToLog("Entrando no bloco Try.");
                    if (Term > 0)
                    {
                        if (CodBarras.Equals(CodBar))
                        {
                            WriteToLog("Produto encontrado: " + CodBar);
                            bool sendResult = bSendProdPrice(Term, Prod, Valor);
                            WriteToLog("Resultado do envio do produto: " + sendResult);
                        }
                        else
                        {
                            WriteToLog("Produto não encontrado: " + CodBar);
                            bool notFoundResult = bSendProdNotFound(Term);
                            WriteToLog("Resultado do envio de 'produto não encontrado': " + notFoundResult);
                        }
                    }
                    WriteToLog("Saindo do bloco Try.");
                }
                catch (Exception ex)
                {
                    WriteToLog("Erro ao processar terminal: " + ex.Message);
                }
            }
            catch (Exception ei)
            {
                WriteToLog("Error no Try externo: " + ei.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(Cod);
                Marshal.FreeHGlobal(Prod);
                Marshal.FreeHGlobal(Valor);
            }

            WriteToLog("TmTerminal_Tick concluído.");

            TmTerminal.Enabled = false;
            WriteToLog("Timer de terminais desabilitado.");

            Timer.Enabled = true;
            WriteToLog("Timer principal reabilitado.");
        }

        public void WriteToLog(string message)
        {
            lock (_LogLock)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
                try
                {
                    using (StreamWriter sw = new StreamWriter(filepath, true))
                    {
                        sw.WriteLine($"{DateTime.Now} : {message}");
                    }
                }
                catch (Exception ex)
                {
                    // Apenas no caso do log falhar
                    EventLog.WriteEntry("Erro ao escrever no log: " + ex.Message, EventLogEntryType.Error);
                }
            }
        }
    }
}
