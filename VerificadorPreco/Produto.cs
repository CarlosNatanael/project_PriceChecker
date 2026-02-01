using System;

namespace VerificadorPreco.Models
{
	public class Produto
	{
		public int Id { get; set; }
		public string Nome { get; set; }
		public string Url { get; set; }
		public decimal PrecoAlvo { get; set; }
		public decimal PrecoAtual { get; set; }
		public string Status { get; set; }
		public DateTime UltimaVerificacao { get; set; }
	}
}