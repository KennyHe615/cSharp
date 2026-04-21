using Moq;

using SharedKernel.Time;


namespace tests.TestSupport.Time;

public static class DateTimeProviderTestFactory
{
    public static readonly DateTimeOffset FixedNow = new DateTimeOffset(2026,
                                                                        1,
                                                                        1,
                                                                        0,
                                                                        0,
                                                                        0,
                                                                        TimeSpan.Zero);

    public static Mock<IDateTimeProvider> Create()
    {
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Loose);

        dateTimeProvider.SetupGet(x => x.UtcNowOffset)
                        .Returns(FixedNow);
        dateTimeProvider.SetupGet(x => x.EstNowOffset)
                        .Returns(FixedNow);
        dateTimeProvider.Setup(x => x.ConvertToEst(It.IsAny<DateTimeOffset>()))
                        .Returns<DateTimeOffset>(x => x);
        dateTimeProvider.Setup(x => x.ConvertToEst(It.IsAny<DateTimeOffset?>()))
                        .Returns<DateTimeOffset?>(x => x);

        return dateTimeProvider;
    }
}
