namespace Sproutoku.Core.Tests.Game.PowerUps;

public class PowerUpSupplyTests {
    [Fact]
    public void EveryGarden_StartsWithTwoWorms_AndNoSeedsOrWater() {
        Assert.Equal(new PowerUpSupply(Seeds: 2, Worm: 2, Water: 2), PowerUpSupply.Starting);
    }

    [Fact]
    public void Counts_CanBeReadByPowerUp() {
        var supply = new PowerUpSupply(Seeds: 1, Worm: 2, Water: 3);

        Assert.Equal(1, supply[PowerUp.Seeds]);
        Assert.Equal(2, supply[PowerUp.Worm]);
        Assert.Equal(3, supply[PowerUp.Water]);
    }

    [Fact]
    public void Spending_TakesOneOfThatPowerUpOnly() {
        var supply = new PowerUpSupply(Seeds: 1, Worm: 2, Water: 3);

        Assert.Equal(new PowerUpSupply(1, 1, 3), supply.Spend(PowerUp.Worm));
        Assert.Equal(new PowerUpSupply(0, 2, 3), supply.Spend(PowerUp.Seeds));
        Assert.Equal(new PowerUpSupply(1, 2, 2), supply.Spend(PowerUp.Water));
        Assert.Equal(new PowerUpSupply(1, 2, 3), supply); // The original doesn't change.
    }

    [Fact]
    public void Spending_WithNoneLeft_Throws() {
        var supply = new PowerUpSupply(Seeds: 0, Worm: 2, Water: 2);

        Assert.False(supply.Has(PowerUp.Seeds));
        Assert.Throws<InvalidOperationException>(() => supply.Spend(PowerUp.Seeds));
    }

    [Fact]
    public void ASupply_IsValid_OnlyWithNoNegativeCounts() {
        Assert.True(PowerUpSupply.IsValid(PowerUpSupply.Starting));
        Assert.True(PowerUpSupply.IsValid(new PowerUpSupply(0, 0, 0)));
        Assert.False(PowerUpSupply.IsValid(new PowerUpSupply(0, -1, 0)));
        Assert.False(PowerUpSupply.IsValid(null));
    }
}
