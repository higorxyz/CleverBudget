# 🔍 Insights Financeiros Automáticos

Os insights financeiros ajudam usuários do CleverBudget a identificar padrões de gastos e oportunidades de economia com base nas transações registradas.

## 📊 Como os Insights Funcionam

Os cálculos analisam dados dos últimos meses (até seis meses antes do período consultado) para comparar o comportamento atual com o histórico do usuário. Os seguintes tipos de insights estão disponíveis na versão inicial:

- **Padrões de Gastos**: identifica categorias com gastos significativamente acima da média recente ou um ritmo mensal projetado acima do histórico.
- **Risco de Orçamento**: monitora orçamentos ativos do mês corrente e sinaliza consumo adiantado em relação ao ritmo esperado.
- **Padrões de Receita**: detecta quedas relevantes ou aumentos incomuns na receita atual.

Cada insight retorna:

- Categoria (`InsightCategory`)
- Severidade (`InsightSeverity`)
- Título e resumo
- Recomendação
- Valores comparativos (impacto x benchmark)
- Dados de apoio (`DataPoints`)

## 🔄 Frequência

Os insights são calculados sob demanda via API. A implementação atual não persiste resultados; cada consulta recalcula os indicadores usando os dados disponíveis. Esse comportamento simplifica a primeira entrega e mantém as informações sempre atualizadas.

## ⚙️ Endpoint

```
GET /api/v2/insights
```

### Parâmetros de Consulta

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `startDate` | `DateTime?` | Data inicial para o recorte de análise. Padrão: início do mês atual menos 3 meses. |
| `endDate` | `DateTime?` | Data final do recorte. Padrão: data atual. |
| `categoryId` | `int?` | Filtra insights de uma categoria específica. |
| `includeIncomeInsights` | `bool` | Inclui análises relacionadas a receitas (padrão: `true`). |
| `includeExpenseInsights` | `bool` | Inclui análises de despesas (padrão: `true`). |

### Resposta Exemplo

```json
[
  {
    "category": "SpendingPattern",
    "severity": "High",
    "title": "Gastos elevados em Restaurantes",
    "summary": "Os gastos atuais estão 60% acima da média dos últimos meses.",
    "recommendation": "Analise quais transações são excepcionais e, se possível, distribua esse custo ao longo dos próximos meses.",
    "impactAmount": 180.0,
    "benchmarkAmount": 300.0,
    "generatedAt": "2025-11-07T12:34:56Z",
    "dataPoints": [
      {
        "label": "Mês atual",
        "value": 480.0,
        "benchmark": 300.0,
        "period": null
      }
    ]
  }
]
```

## 🔮 Próximos Passos

- Persistir histórico de insights para exibir evolução.
- Agendar geração automática e envio por email ou notificações push.
- Adicionar insights de metas e conquistas (gamificação).
- Integrar previsões de gastos usando modelos estatísticos.

---

> Atualizado em novembro de 2025.
