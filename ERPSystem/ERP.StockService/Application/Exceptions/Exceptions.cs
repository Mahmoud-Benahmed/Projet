namespace ERP.StockService.Application.Exceptions;
public class FournisseurNotFoundException(Guid id)
    : KeyNotFoundException($"Fournisseur '{id}' was not found.");

public class FournisseurBlockedException(Guid id)
    : InvalidOperationException($"Fournisseur '{id}' is blocked.");

public class BonEntreNotFoundException(Guid id)
    : KeyNotFoundException($"BonEntre '{id}' was not found.");

public class BonSortieNotFoundException(Guid id)
    : KeyNotFoundException($"BonSortie '{id}' was not found.");

public class BonRetourNotFoundException(Guid id)
    : KeyNotFoundException($"BonRetour '{id}' was not found.");

public class ArticleNotInSourceBonException : Exception
{
    public ArticleNotInSourceBonException(Guid articleId, Guid sourceId)
        : base($"Article '{articleId}' was not found in source bon '{sourceId}'.")
    {
    }
}
public class RetourQuantityExceedsSourceException : Exception
{
    public RetourQuantityExceedsSourceException(Guid articleId, decimal requested, decimal max)
        : base($"Returned quantity {requested} for article '{articleId}' exceeds source quantity of {max}.")
    {
    }
}