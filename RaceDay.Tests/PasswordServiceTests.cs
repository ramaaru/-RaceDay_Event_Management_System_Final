using RaceDay.API.Models;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void Hash_And_Verify_ValidPassword_Succeeds()
    {
        var service=new PasswordService();
        var user=new User{Email="test@example.com"};
        var hash=service.Hash(user,"Password123!");
        Assert.NotEqual("Password123!",hash);
        Assert.True(service.Verify(user,hash,"Password123!"));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var service=new PasswordService();
        var user=new User{Email="test@example.com"};
        var hash=service.Hash(user,"Password123!");
        Assert.False(service.Verify(user,hash,"WrongPassword!"));
    }
}
