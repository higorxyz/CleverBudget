namespace CleverBudget.Core.Enums;

/// <summary>
/// Frequência de recorrência de transações
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// Diária - Repetir todos os dias
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Semanal - Repetir toda semana no mesmo dia
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Mensal - Repetir todo mês no mesmo dia
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Anual - Repetir todo ano na mesma data
    /// </summary>
    Yearly = 4
}