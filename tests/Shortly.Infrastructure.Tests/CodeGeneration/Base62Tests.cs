using Shortly.Infrastructure.CodeGeneration;

namespace Shortly.Infrastructure.Tests.CodeGeneration;

public sealed class Base62Tests
{
    // --- Encode ---

    [Fact]
    public void Encode_Zero_ReturnsZeroString()
    {
        Assert.Equal("0", Base62.Encode(0));
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(35, "Z")]
    [InlineData(36, "a")]
    [InlineData(61, "z")]
    public void Encode_SingleDigitValues_ReturnsExpectedCharacter(long value, string expected)
    {
        Assert.Equal(expected, Base62.Encode(value));
    }

    [Theory]
    [InlineData(62, "10")]
    [InlineData(63, "11")]
    [InlineData(3843, "zz")]      // 61*62 + 61
    [InlineData(3844, "100")]     // 62^2
    public void Encode_MultiDigitValues_ReturnsExpected(long value, string expected)
    {
        Assert.Equal(expected, Base62.Encode(value));
    }

    [Fact]
    public void Encode_LargeValue_ProducesNonEmptyString()
    {
        var result = Base62.Encode(100_000_000);

        Assert.False(string.IsNullOrEmpty(result));
        Assert.True(result.Length >= 5, $"Expected at least 5 chars for 100M, got {result.Length}: '{result}'");
    }

    [Fact]
    public void Encode_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62.Encode(-1));
    }

    [Fact]
    public void Encode_MaxLong_DoesNotThrow()
    {
        var result = Base62.Encode(long.MaxValue);

        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void Encode_ResultContainsOnlyBase62Characters()
    {
        var values = new long[] { 0, 1, 62, 3844, 100_000_000, long.MaxValue };

        foreach (var value in values)
        {
            var encoded = Base62.Encode(value);
            Assert.Matches(@"^[0-9A-Za-z]+$", encoded);
        }
    }

    // --- Decode ---

    [Fact]
    public void Decode_ZeroString_ReturnsZero()
    {
        Assert.Equal(0, Base62.Decode("0"));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("9", 9)]
    [InlineData("A", 10)]
    [InlineData("Z", 35)]
    [InlineData("a", 36)]
    [InlineData("z", 61)]
    public void Decode_SingleCharacter_ReturnsExpectedValue(string encoded, long expected)
    {
        Assert.Equal(expected, Base62.Decode(encoded));
    }

    [Theory]
    [InlineData("10", 62)]
    [InlineData("11", 63)]
    [InlineData("zz", 3843)]
    [InlineData("100", 3844)]
    public void Decode_MultiCharacter_ReturnsExpectedValue(string encoded, long expected)
    {
        Assert.Equal(expected, Base62.Decode(encoded));
    }

    [Fact]
    public void Decode_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62.Decode(""));
    }

    [Fact]
    public void Decode_NullString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62.Decode(null!));
    }

    [Theory]
    [InlineData("abc!")]
    [InlineData("test@")]
    [InlineData("hello world")]
    [InlineData("-1")]
    [InlineData("abc+def")]
    public void Decode_InvalidCharacters_ThrowsArgumentException(string encoded)
    {
        var ex = Assert.Throws<ArgumentException>(() => Base62.Decode(encoded));
        Assert.Contains("Invalid Base62 character", ex.Message);
    }

    // --- Roundtrip ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(61)]
    [InlineData(62)]
    [InlineData(3843)]
    [InlineData(3844)]
    [InlineData(100_000_000)]
    [InlineData(999_999_999)]
    [InlineData(long.MaxValue)]
    public void Roundtrip_EncodeDecodeReturnsOriginalValue(long original)
    {
        var encoded = Base62.Encode(original);
        var decoded = Base62.Decode(encoded);

        Assert.Equal(original, decoded);
    }

    // --- ShortCode compatibility ---

    [Fact]
    public void Encode_InitialCounterRange_ProducesValidShortCodeLength()
    {
        // RangeBasedCodeGenerator starts at 100_000_000
        // ShortCode requires 5-8 Base62 characters
        for (long i = 100_000_000; i < 100_000_100; i++)
        {
            var encoded = Base62.Encode(i);
            Assert.InRange(encoded.Length, 5, 8);
        }
    }

    [Fact]
    public void Encode_ConsecutiveValues_ProduceUniqueResults()
    {
        var results = new HashSet<string>();
        for (long i = 100_000_000; i < 100_001_000; i++)
        {
            Assert.True(results.Add(Base62.Encode(i)), $"Duplicate encoding at {i}");
        }
    }

    [Fact]
    public void Encode_ConsecutiveValues_AreMonotonicallyIncreasingLexicographically()
    {
        // Same-length Base62 strings should sort in the same order as their numeric values
        var prev = Base62.Encode(100_000_000);
        for (long i = 100_000_001; i < 100_000_100; i++)
        {
            var current = Base62.Encode(i);
            Assert.True(
                string.CompareOrdinal(current, prev) > 0,
                $"Expected '{current}' > '{prev}' for value {i}");
            prev = current;
        }
    }
}
