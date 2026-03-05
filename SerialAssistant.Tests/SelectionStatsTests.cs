using SerialAssistant.ViewModels;
using Xunit;

namespace SerialAssistant.Tests;

public class SelectionStatsTests
{
    [Fact]
    public void UpdateRxSelectionStats_WithText_UpdatesCharCountAndHexCount()
    {
        var vm = new MainWindowViewModel();

        // "hello" has 5 chars, so 5/2 = 2 potential hex bytes
        vm.UpdateRxSelectionStats("hello", 5);

        Assert.Equal(5, vm.RxSelectionLength);
        Assert.Equal(5, vm.RxSelectionCharCount);
        Assert.Equal(2, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithHexText_UpdatesCorrectHexCount()
    {
        var vm = new MainWindowViewModel();

        // "48656C6C6F" = "Hello" in hex (10 chars = 5 bytes)
        vm.UpdateRxSelectionStats("48656C6C6F", 10);

        Assert.Equal(10, vm.RxSelectionLength);
        Assert.Equal(10, vm.RxSelectionCharCount);
        Assert.Equal(5, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithNullText_ResetsCounts()
    {
        var vm = new MainWindowViewModel();
        vm.UpdateRxSelectionStats("hello", 5);

        vm.UpdateRxSelectionStats(null, 0);

        Assert.Equal(0, vm.RxSelectionLength);
        Assert.Equal(0, vm.RxSelectionCharCount);
        Assert.Equal(0, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithEmptyText_ResetsCounts()
    {
        var vm = new MainWindowViewModel();
        vm.UpdateRxSelectionStats("hello", 5);

        vm.UpdateRxSelectionStats("", 0);

        Assert.Equal(0, vm.RxSelectionLength);
        Assert.Equal(0, vm.RxSelectionCharCount);
        Assert.Equal(0, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateTxSelectionStats_WithText_UpdatesCharCountAndHexCount()
    {
        var vm = new MainWindowViewModel();

        // "test" has 4 chars, so 4/2 = 2 potential hex bytes
        vm.UpdateTxSelectionStats("test", 4);

        Assert.Equal(4, vm.TxSelectionLength);
        Assert.Equal(4, vm.TxSelectionCharCount);
        Assert.Equal(2, vm.TxSelectionHexCount);
    }

    [Fact]
    public void UpdateTxSelectionStats_WithHexText_UpdatesCorrectHexCount()
    {
        var vm = new MainWindowViewModel();

        // "74657374" = "test" in hex (8 chars = 4 bytes)
        vm.UpdateTxSelectionStats("74657374", 8);

        Assert.Equal(8, vm.TxSelectionLength);
        Assert.Equal(8, vm.TxSelectionCharCount);
        Assert.Equal(4, vm.TxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithMixedHex_HandlesCorrectly()
    {
        var vm = new MainWindowViewModel();

        // "ABCD" = 2 bytes
        vm.UpdateRxSelectionStats("ABCD", 4);

        Assert.Equal(4, vm.RxSelectionCharCount);
        Assert.Equal(2, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithNewlines_ExcludedFromHexCount()
    {
        var vm = new MainWindowViewModel();

        // "ABCD" after removing \n and \r = 4 chars = 2 bytes
        vm.UpdateRxSelectionStats("AB\nCD", 5);

        Assert.Equal(5, vm.RxSelectionCharCount);
        Assert.Equal(2, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithDashes_ExcludedFromHexCount()
    {
        var vm = new MainWindowViewModel();

        // "ABCD" after removing "-" = 4 chars = 2 bytes
        vm.UpdateRxSelectionStats("AB-CD", 5);

        Assert.Equal(5, vm.RxSelectionCharCount);
        Assert.Equal(2, vm.RxSelectionHexCount);
    }

    [Fact]
    public void SelectionStats_DefaultValues_AreZero()
    {
        var vm = new MainWindowViewModel();

        Assert.Equal(0, vm.RxSelectionLength);
        Assert.Equal(0, vm.TxSelectionLength);
        Assert.Equal(0, vm.RxSelectionCharCount);
        Assert.Equal(0, vm.RxSelectionHexCount);
        Assert.Equal(0, vm.TxSelectionCharCount);
        Assert.Equal(0, vm.TxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_ConsecutiveUpdates_WorkCorrectly()
    {
        var vm = new MainWindowViewModel();

        vm.UpdateRxSelectionStats("first", 5);
        Assert.Equal(5, vm.RxSelectionCharCount);

        vm.UpdateRxSelectionStats("second", 6);
        Assert.Equal(6, vm.RxSelectionCharCount);

        vm.UpdateRxSelectionStats("longer text", 11);
        Assert.Equal(11, vm.RxSelectionCharCount);
    }

    [Fact]
    public void UpdateTxSelectionStats_ConsecutiveUpdates_WorkCorrectly()
    {
        var vm = new MainWindowViewModel();

        vm.UpdateTxSelectionStats("a", 1);
        Assert.Equal(1, vm.TxSelectionCharCount);

        vm.UpdateTxSelectionStats("ab", 2);
        Assert.Equal(2, vm.TxSelectionCharCount);

        vm.UpdateTxSelectionStats("abc", 3);
        Assert.Equal(3, vm.TxSelectionCharCount);
    }

    [Fact]
    public void RxAndTxSelectionStats_IndependentUpdates_WorkCorrectly()
    {
        var vm = new MainWindowViewModel();

        vm.UpdateRxSelectionStats("rx text", 7);
        Assert.Equal(7, vm.RxSelectionCharCount);
        Assert.Equal(0, vm.TxSelectionCharCount);

        vm.UpdateTxSelectionStats("tx text", 7);
        Assert.Equal(7, vm.RxSelectionCharCount);
        Assert.Equal(7, vm.TxSelectionCharCount);

        vm.UpdateRxSelectionStats("", 0);
        Assert.Equal(0, vm.RxSelectionCharCount);
        Assert.Equal(7, vm.TxSelectionCharCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithOddHexLength_HandlesCorrectly()
    {
        var vm = new MainWindowViewModel();

        // Odd length hex string: 5 chars / 2 = 2 bytes (floor)
        vm.UpdateRxSelectionStats("ABCDE", 5);

        Assert.Equal(5, vm.RxSelectionCharCount);
        Assert.Equal(2, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_LargeHexCount_CalculatesCorrectly()
    {
        var vm = new MainWindowViewModel();

        // 100 bytes in hex = 200 hex chars
        var hexText = new string('A', 200);
        vm.UpdateRxSelectionStats(hexText, 200);

        Assert.Equal(200, vm.RxSelectionCharCount);
        Assert.Equal(100, vm.RxSelectionHexCount);
    }

    [Fact]
    public void UpdateRxSelectionStats_WithSpacesInHex_CalculatesCorrectly()
    {
        var vm = new MainWindowViewModel();

        // "48 65 6C 6C 6F" = "Hello" with spaces (14 chars = 5 bytes after removing spaces)
        vm.UpdateRxSelectionStats("48 65 6C 6C 6F", 14);

        Assert.Equal(14, vm.RxSelectionCharCount);
        Assert.Equal(5, vm.RxSelectionHexCount);
    }
}
