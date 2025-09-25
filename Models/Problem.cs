namespace ScspApi.Models
{
    public enum ProblemStatus
    {
        Open,
        InProgress,
        Solved,
        Recurring,
        Rejected
    }

    public enum ProblemType 
    {
        Buraco,               // Buracos nas vias públicas
        Iluminacao,           // Problemas com iluminação pública (lâmpadas queimadas, falta de iluminação)
        Lixo,                 // Acúmulo de lixo nas ruas ou em áreas públicas
        Esgoto,               // Esgotos entupidos ou com vazamento
        Pavimentacao,         // Pavimentação danificada ou mal conservada
        Arvore,               // Árvores caídas ou com risco de queda
        TransportePublico,    // Problemas no transporte público (atrasos, falhas, superlotação)
        PostoSaude,
        EscolaPublica,
        Deslizamento,         // Deslizamentos de terra, especialmente em áreas de risco
        Enchente,             // Alagamentos ou enchentes em vias públicas
        Sinalizacao,          // Falta ou defeito na sinalização de trânsito (placas, semáforos)
        FiosExpostos,         // Fios ou cabos expostos em áreas públicas
        RiscoDeIncendio,      // Riscos de incêndio em áreas públicas ou privadas
        FaltaDeAcessibilidade,// Falta de acessibilidade para pessoas com deficiência
        PoluicaoSonora,       // Poluição sonora em áreas públicas
        PoluicaoAr,           // Poluição do ar (fumaça, gases tóxicos)
        Vazamento,            // Vazamento de água em ruas ou imóveis públicos
        Saneamento,           // Falta de saneamento básico em algumas áreas (esgoto, coleta de lixo)
        Animais,              // Animais soltos nas ruas, como cães, gatos ou animais maiores
        Vandalismo,           // Vandalismo em bens públicos (pichação, destruição de patrimônio)
        FaltaDeVagaEstacionamento, // Falta de vagas de estacionamento ou problemas de estacionamento irregular
        ConstrucaoIrregular,  // Construções irregulares ou ilegais (sem alvará, etc.)
        Calcada,                // Buracos ou defeitos nas calçadas
        BarulhoExcessivo,     // Barulho excessivo em horários inadequados (geradores, festas)
        FaltasDeLimpeza,      // Falta de limpeza em áreas públicas, como praças e ruas
        MalAparelhoPublico,   // Problemas com aparelhos públicos de uso comum (bancos, lixeiras, etc.)
        FaltaDeÁgua,          // Falta de fornecimento de água em algumas regiões
        Outro = 130
    }

    public class Problem
    {
        public int Id { get; set; }
        public ProblemType Type { get; set; } // "Buraco", "Iluminação", "Árvore caida", etc. TODO: Fazer uma lista pre-definida.
        public string Description { get; set; } // Descriçãodo problema
        public string Address { get; set; } // Endereço de referencia visual
        public double Latitude { get; set; } // Localização
        public double Longitude { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SolvedAt { get; set; } = null; // Data em que foi resolvido
        public DateTime? RecurringAt { get; set; } = null; // Data em que foi reportado como recorrente
        public string ImageUrl { get; set; } = ""; // URL da imagem principal
        public string ImageSolvedUrl { get; set; } = ""; // URL da imagem de resolução
        public string ImageRecurringUrl { get; set; } = ""; // URL da imagem de recorrente
        public List<string>? ImageUrls { get; set; } = null; // URLs das imagens adicionais
        public ProblemStatus Status { get; set; } = ProblemStatus.Open;
        public string UserId { get; set; } // Identificador do usuário que reportou
        public string UserIdSolved { get; set; } = ""; // Identificador do usuário que resolveu
        public string UserIdRecurring { get; set; } = ""; // Identificador do usuário que reportou como recorrente
        public int Upvotes { get; set; } = 0; // Votos positivos
        public int Downvotes { get; set; } = 0; // Votos negativos
        public string OfficialResponse { get; set; } = ""; // Resposta oficial da prefeitura ou órgão responsável
        public DateTime? OfficialResponseAt { get; set; } = null; // Data da resposta oficial
        public string OfficialResponderId { get; set; } = ""; // Identificador do usuário oficial que respondeu
    }
}
