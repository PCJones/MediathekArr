namespace MediathekArr.Exceptions;

public class SeriesNotFoundException : Exception
{
    #region Constructors
    public SeriesNotFoundException(int tvdbId) => TvdbId = tvdbId;

    public SeriesNotFoundException(int tvdbId, string message) : base(message) => TvdbId = tvdbId;

    public SeriesNotFoundException(int tvdbId, string message, Exception innerException) : base(message, innerException) => TvdbId = tvdbId;
    #endregion

    #region Properties
    public int TvdbId { get; }
    #endregion
}
