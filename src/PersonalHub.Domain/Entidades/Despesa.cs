using System.ComponentModel.DataAnnotations;

namespace PersonalHub.Domain.Entidades
{
    public class Despesa : Entity
    {
        protected Despesa()
        {
            Parcelas = this.Parcelas ?? new List<Parcela>();
            TagsDespesas = new List<TagDespesa>();
            UsuariosEnvolvidos = new List<UsuarioDespesa>();
        }

        [Required]
        public string? Titulo { get; set; }

        public string Descritivo { get; set; } = string.Empty;

        [Required]
        public int? QtdParcelas { get; set; }

        [Required]
        public decimal? Valor { get; set; }

        [Required]
        public DateTime Vencimento { get; set; }

        public bool Recorrente { get; set; }
        
        public List<Parcela> Parcelas { get; set; }
        
        public List<UsuarioDespesa> UsuariosEnvolvidos { get; set; }
        
        public List<TagDespesa> TagsDespesas { get; set; }

        public string ObterVencimentoFormatado() => this.Vencimento.ToString("dd/MM/yyyy");

        private void AdicionarUsuario(string usuarioId)
        {
            if (this.UsuariosEnvolvidos == null)
                this.UsuariosEnvolvidos = new List<UsuarioDespesa>();

            if (!this.UsuariosEnvolvidos.Any(e => e.UsuarioId == usuarioId))
                this.UsuariosEnvolvidos.Add(new UsuarioDespesa(usuarioId));
        }

        private void AdicionarParcela(int numero, decimal valor, DateTime vencimento)
        {
            Parcelas.Add(new Parcela(numero, valor, vencimento.AddMonths(numero - 1)));
            Parcelas = Parcelas.OrderBy(e => e.Vencimento).ToList();
        }

        public Despesa(int id, string? titulo, int? qtdParcelas, decimal? valorParcela, string descritivo, 
                       DateTime vencimento, List<string> usuarios, List<Tag> tags)
        {
            if (qtdParcelas <= 0) throw new ArgumentException("Você deve informar uma qtd de parcelas >= 1.");

            if (valorParcela <= 0) throw new ArgumentException("Você deve informar um valor > 0.");

            Parcelas = this.Parcelas ?? new List<Parcela>();
            TagsDespesas = new List<TagDespesa>();
            UsuariosEnvolvidos = new List<UsuarioDespesa>();

            Id = id;
            Titulo = titulo;   
            Descritivo = descritivo;   
            Valor =  valorParcela *  qtdParcelas;
            Vencimento = vencimento;
            QtdParcelas = qtdParcelas;

            foreach (var tag in tags)
                this.AdicionarTag(tag);

            foreach (var usuarioId in usuarios)
                this.AdicionarUsuario(usuarioId);

            CriarParcelas(qtdParcelas ?? 0, valorParcela ?? 0, vencimento);
        }

        private void AdicionarTag(Tag tag)
        {
            if (!this.TagsDespesas.Any(e => e.TagId == tag.Id && e.DespesaId == this.Id))
                this.TagsDespesas.Add(new TagDespesa { DespesaId = this.Id, TagId = tag.Id });
        }

        private void CriarParcelas(int qtdParcelas, decimal valorParcela, DateTime vencimento)
        {
            if (qtdParcelas == 0) return;

            if (qtdParcelas > 1)
            {
                AdicionarParcela(1, valorParcela, vencimento);

                for (int numeroParcela = 2; numeroParcela <= qtdParcelas; numeroParcela++)
                    AdicionarParcela(numeroParcela, valorParcela, vencimento);

                return;
            }

            AdicionarParcela(1, valorParcela, vencimento);
        }

        public static Despesa Criar(IDespesa model)
        {
            return new Despesa(
                model.Id,
                model.Titulo, 
                model.QtdParcelas,
                model.ValorParcela, 
                model.Descritivo ?? string.Empty,
                model.Vencimento, 
                model.UsuariosEnvolvidos,
                model.TagsDespesa
            ); 
        }

        public static Despesa CriarRecorrente(IDespesa model)
        {
            var despesa = new Despesa(
                model.Id,
                model.Titulo,
                1,
                model.ValorParcela,
                model.Descritivo ?? string.Empty,
                model.Vencimento,
                model.UsuariosEnvolvidos,
                model.TagsDespesa
            );

            despesa.Parcelas.Clear();
            despesa.QtdParcelas = 0;
            despesa.Valor = model.ValorParcela;
            despesa.Recorrente = true;

            return despesa;
        }

        public DateTime ObterVencimentoRecorrente(DateTime mesReferencia)
        {
            int dia = Math.Min(Vencimento.Day, DateTime.DaysInMonth(mesReferencia.Year, mesReferencia.Month));
            return new DateTime(mesReferencia.Year, mesReferencia.Month, dia);
        }

        public void Atualizar(IDespesa model)
        {
            this.Titulo = model.Titulo; 
            this.Descritivo = model.Descritivo ?? string.Empty;
            this.Valor = Recorrente
                ? model.ValorParcela
                : model.ValorParcela * model.QtdParcelas;
            this.QtdParcelas = Recorrente ? 0 : model.QtdParcelas;
            this.Vencimento = model.Vencimento;

            // atualizando usuarios...

            var idsUsuariosAtuais = this.UsuariosEnvolvidos.Select(e => e.UsuarioId);

            if (model.UsuariosEnvolvidos.Count > 0)
                foreach (var usuarioId in model.UsuariosEnvolvidos)
                    this.AdicionarUsuario(usuarioId);

            var usuariosRemover = new List<string>();

            foreach (string usuarioId in idsUsuariosAtuais)
                if (!model.UsuariosEnvolvidos.Any(e => e == usuarioId))
                    usuariosRemover.Add(usuarioId);

            this.UsuariosEnvolvidos = this.UsuariosEnvolvidos
                .Where(usuarioEnvolvidoAtual => !usuariosRemover.Contains(usuarioEnvolvidoAtual.UsuarioId))
                .ToList();

            // atualizando tags...
            var tagsRemover = new List<string>();

            var tagsAtuais = this.TagsDespesas != null ? this.TagsDespesas.Select(e =>e.Tag.Nome).ToList() : new List<string>();

            foreach (string tagNome in tagsAtuais)
                if (!model.TagsDespesa.Any(e => e.Nome == tagNome))
                    tagsRemover.Add(tagNome);

            this.TagsDespesas = (this.TagsDespesas ?? new List<TagDespesa>())
                .Where(tag => !tagsRemover.Contains(tag.Tag.Nome))
                .ToList();

            if (model.TagsDespesa.Count > 0)
            {
                foreach (var tagSelecionada in model.TagsDespesa)
                    if (!this.TagsDespesas.Any(e => e.TagId == tagSelecionada.Id))
                        this.AdicionarTag(tagSelecionada);
            }
        }

        public List<Parcela> AjustarQuantidadeParcelas(int quantidade, decimal valorNovaParcela)
        {
            if (quantidade < 1)
                throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade de parcelas deve ser pelo menos 1.");

            var removidas = Parcelas
                .Where(parcela => parcela.Numero > quantidade)
                .OrderByDescending(parcela => parcela.Numero)
                .ToList();

            foreach (var parcela in removidas)
                Parcelas.Remove(parcela);

            var parcelasExistentes = Parcelas.ToDictionary(parcela => parcela.Numero);
            for (int numero = 1; numero <= quantidade; numero++)
            {
                if (parcelasExistentes.ContainsKey(numero))
                    continue;

                DateTime vencimentoNovaParcela;
                if (parcelasExistentes.TryGetValue(numero - 1, out var parcelaAnterior))
                    vencimentoNovaParcela = parcelaAnterior.Vencimento.AddMonths(1);
                else if (parcelasExistentes.TryGetValue(numero + 1, out var proximaParcela))
                    vencimentoNovaParcela = proximaParcela.Vencimento.AddMonths(-1);
                else
                    vencimentoNovaParcela = Vencimento.AddMonths(numero - 1);

                var novaParcela = new Parcela(numero, valorNovaParcela, vencimentoNovaParcela);
                Parcelas.Add(novaParcela);
                parcelasExistentes[numero] = novaParcela;
            }

            QtdParcelas = quantidade;
            Valor = Parcelas.Sum(parcela => parcela.Valor);
            return removidas;
        }

        public Despesa Clonar(DateTime vencimento)
        {
            var novaDespesa = new Despesa(
                0,
                this.Titulo,
                this.QtdParcelas,
                this.Valor,
                this.Descritivo,
                vencimento,
                this.UsuariosEnvolvidos.Select(e => e.UsuarioId).ToList(),
                this.TagsDespesas.Select(e => new Tag { Id = e.TagId }).ToList()
            );

            return novaDespesa;
        }
    }
}
