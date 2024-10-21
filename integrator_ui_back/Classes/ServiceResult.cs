namespace Teamwork.Integrator.Core.Classes;

/// <summary>
/// Represent result of operation.
/// </summary>
public class ServiceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceResult"/> class.
    /// </summary>
    /// <param name="isSuccess">if set to <c>true</c> [is success].</param>
    /// <param name="errorMessage">The error message.</param>
    protected ServiceResult(bool isSuccess, string errorMessage = default)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this instance is success.
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Creates result in case of success.
    /// </summary>
    /// <returns><see cref="ServiceResult"/></returns>
    public static ServiceResult FromSuccess()
    {
        return new ServiceResult(true);
    }

    /// <summary>
    /// Creates result when error occurred.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns><see cref="ServiceResult"/></returns>
    public static ServiceResult FromError(string errorMessage = null)
    {
        return new ServiceResult(false, errorMessage: errorMessage);
    }
}

/// <summary>
/// Represent result of operation with returnable object.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public class ServiceResult<TEntity> : ServiceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceResult{TEntity}"/> class.
    /// </summary>
    /// <param name="isSuccess">if set to <c>true</c> [is success].</param>
    /// <param name="entity">The entity.</param>
    /// <param name="errorMessage">The error message.</param>
    private ServiceResult(bool isSuccess, TEntity entity = default, string errorMessage = default)
        : base(isSuccess, errorMessage)
    {
        this.Result = entity;
    }

    /// <summary>
    /// Gets or sets the entity.
    /// </summary>
    public TEntity Result { get; protected set; }

    /// <summary>
    /// Creates result in case of success.
    /// </summary>
    /// <returns><see cref="ServiceResult{TEntity}"/></returns>
    public static ServiceResult<TEntity> FromSuccess(TEntity entity)
    {
        return new ServiceResult<TEntity>(true, entity);
    }

    /// <summary>
    /// Creates result when error occurred.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns><see cref="ServiceResult{TEntity}"/></returns>
    public static new ServiceResult<TEntity> FromError(string errorMessage = null)
    {
        return new ServiceResult<TEntity>(false, errorMessage: errorMessage);
    }
}