using System.Reflection;

using Infrastructure.Persistence.Repositories.SyncTracking;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;


namespace tests.Unit.Infrastructure.Persistence.Repositories.SyncTracking;

public sealed class UniqueViolationDetectorTests
{
    #region ========== *** IsScopeKeyUniqueViolation *** ==========

    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void IsScopeKeyUniqueViolation_WhenSqlDuplicateKeyCode_ReturnsTrue(int sqlNumber)
    {
        SqlException sqlException = CreateSqlException(sqlNumber, "duplicate key");
        DbUpdateException dbUpdateException = new DbUpdateException("save failed", sqlException);

        bool actual = UniqueViolationDetector.IsScopeKeyUniqueViolation(dbUpdateException);

        Assert.True(actual);
    }

    [Fact]
    public void IsScopeKeyUniqueViolation_WhenScopeTokenInMessage_ReturnsTrue()
    {
        DbUpdateException dbUpdateException = new DbUpdateException("UX_sync_request_scope_key violated");

        bool actual = UniqueViolationDetector.IsScopeKeyUniqueViolation(dbUpdateException);

        Assert.True(actual);
    }

    [Fact]
    public void IsScopeKeyUniqueViolation_WhenNoSignal_ReturnsFalse()
    {
        DbUpdateException dbUpdateException = new DbUpdateException("some other failure");

        bool actual = UniqueViolationDetector.IsScopeKeyUniqueViolation(dbUpdateException);

        Assert.False(actual);
    }

    #endregion

    #region ========== *** IsCheckpointUniqueViolation *** ==========

    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void IsCheckpointUniqueViolation_WhenSqlDuplicateKeyCode_ReturnsTrue(int sqlNumber)
    {
        SqlException sqlException = CreateSqlException(sqlNumber, "duplicate key");
        DbUpdateException dbUpdateException = new DbUpdateException("save failed", sqlException);

        bool actual = UniqueViolationDetector.IsCheckpointUniqueViolation(dbUpdateException);

        Assert.True(actual);
    }

    [Fact]
    public void IsCheckpointUniqueViolation_WhenCheckpointTokenInMessage_ReturnsTrue()
    {
        DbUpdateException dbUpdateException = new DbUpdateException("UX_sync_checkpoint_run_step_cursor violated");

        bool actual = UniqueViolationDetector.IsCheckpointUniqueViolation(dbUpdateException);

        Assert.True(actual);
    }

    [Fact]
    public void IsCheckpointUniqueViolation_WhenNoSignal_ReturnsFalse()
    {
        DbUpdateException dbUpdateException = new DbUpdateException("some other failure");

        bool actual = UniqueViolationDetector.IsCheckpointUniqueViolation(dbUpdateException);

        Assert.False(actual);
    }

    #endregion

    #region ========== *** Private Section *** ==========

    private static SqlException CreateSqlException(int number, string message)
    {
        object errorCollection = Activator.CreateInstance(typeof(SqlErrorCollection), true)
                                 ?? throw new InvalidOperationException("Failed to create SqlErrorCollection.");

        ConstructorInfo sqlErrorConstructor = typeof(SqlError)
                                             .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                                             .OrderByDescending(c => c.GetParameters()
                                                                      .Length)
                                             .First();

        object?[] sqlErrorArgs = BuildArguments(sqlErrorConstructor.GetParameters(),
                                                number,
                                                message,
                                                null);
        object sqlError = sqlErrorConstructor.Invoke(sqlErrorArgs);

        MethodInfo addMethod =
            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SqlErrorCollection.Add not found.");

        addMethod.Invoke(errorCollection, [sqlError]);

        ConstructorInfo sqlExceptionConstructor = typeof(SqlException)
                                                 .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                                                 .First(c =>
                                                        {
                                                            ParameterInfo[] parameters = c.GetParameters();

                                                            return parameters.Length >= 2
                                                                   && parameters[1].ParameterType
                                                                   == typeof(SqlErrorCollection);
                                                        });

        object?[] sqlExceptionArgs = BuildArguments(sqlExceptionConstructor.GetParameters(),
                                                    number,
                                                    message,
                                                    errorCollection);

        object sqlException = sqlExceptionConstructor.Invoke(sqlExceptionArgs);

        return (SqlException)sqlException;
    }

    private static object?[] BuildArguments(ParameterInfo[] parameters,
                                            int number,
                                            string message,
                                            object? errorCollection)
    {
        object?[] args = new object?[parameters.Length];

        for (int index = 0; index < parameters.Length; index++)
        {
            Type type = parameters[index].ParameterType;

            if (index == 0 && type == typeof(int))
            {
                args[index] = number;

                continue;
            }

            if (type == typeof(string))
            {
                args[index] = message;

                continue;
            }

            if (errorCollection is not null && type == typeof(SqlErrorCollection))
            {
                args[index] = errorCollection;

                continue;
            }

            args[index] =
                type == typeof(byte) ? (byte)0 :
                type == typeof(short) ? (short)0 :
                type == typeof(int) ? 0 :
                type == typeof(uint) ? 0u :
                type == typeof(long) ? 0L :
                type == typeof(bool) ? false :
                type == typeof(Guid) ? Guid.NewGuid() :
                type == typeof(Exception) ? null :
                type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        return args;
    }

    #endregion
}
