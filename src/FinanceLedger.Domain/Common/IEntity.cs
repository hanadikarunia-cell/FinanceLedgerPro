namespace FinanceLedger.Domain.Common;

/// <summary>
/// Marker contract for all persisted aggregate roots. Every entity exposes a
/// string <see cref="Id"/> that also serves as its Postgres primary key.
/// </summary>
public interface IEntity
{
    string Id { get; set; }
}
