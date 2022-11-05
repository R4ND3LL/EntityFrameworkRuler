//using Microsoft.AspNet.Identity;
using Moq;
using NUnit.Framework;
using WealthTrader.Domain.Exceptions;
using System.Threading.Tasks;
using WealthTrader.Application.Services;
using WealthTrader.Application.Services.AuthenticationServices;
using WealthTrader.Application.WealthSimple.Api;
using WealthTrader.Domain.Entities;
using WealthTrader.Domain.TradeProvider;

namespace WealthTrader.Domain.Tests.Services.AuthenticationServices {
    [TestFixture]
    public class AuthenticationServiceTests {
        //private Mock<IPasswordHasher> mockPasswordHasher;
        //private Mock<INativeUserService> mockAccountService;
        private Mock<IWealthSimpleTradeSessionFactory> mockLogin;
        private AuthenticationService localDatabaseAuthenticationService;

        [SetUp]
        public void SetUp() {
            //mockPasswordHasher = new Mock<IPasswordHasher>();
            //mockAccountService = new Mock<INativeUserService>();
            mockLogin = new Mock<IWealthSimpleTradeSessionFactory>();
            localDatabaseAuthenticationService = new AuthenticationService(null, mockLogin.Object);
        }

        [Test]
        public async Task Login_WithCorrectPasswordForExistingUsername_ReturnsAccountForCorrectUsername() {
            string expectedUsername = "testuser";
            string password = "testpassword";
            // _mockAccountService.Setup(s => s.GetByUsername(expectedUsername)).ReturnsAsync(new DbAccount() {AccountHolder = new DbUser() {Username = expectedUsername}});
            // _mockPasswordHasher.Setup(s => s.VerifyHashedPassword(It.IsAny<string>(), password)).Returns(PasswordVerificationResult.Success);
            //
            // DbAccount account = (await localDatabaseAuthenticationService.LoginAsync(TradeProvider.TradeProviderType.Demo, expectedUsername, password))?.User;
            //
            // string actualUsername = account.AccountHolder.Username;
            // Assert.AreEqual(expectedUsername, actualUsername);
        }

        [Test]
        public void Login_WithIncorrectPasswordForExistingUsername_ThrowsInvalidPasswordExceptionForUsername() {
            string expectedUsername = "testuser";
            string password = "testpassword";
            // _mockAccountService.Setup(s => s.GetByUsername(expectedUsername)).ReturnsAsync(new DbAccount() {AccountHolder = new DbUser() {Username = expectedUsername}});
            // _mockPasswordHasher.Setup(s => s.VerifyHashedPassword(It.IsAny<string>(), password)).Returns(PasswordVerificationResult.Failed);
            //
            // InvalidPasswordException exception = Assert.ThrowsAsync<InvalidPasswordException>(() => localDatabaseAuthenticationService.LoginAsync(TradeProvider.TradeProviderType.Demo, expectedUsername, password));
            //
            // string actualUsername = exception.Username;
            // Assert.AreEqual(expectedUsername, actualUsername);
        }

        [Test]
        public void Login_WithNonExistingUsername_ThrowsInvalidPasswordExceptionForUsername() {
            string expectedUsername = "testuser";
            string password = "testpassword";
            //mockPasswordHasher.Setup(s => s.VerifyHashedPassword(It.IsAny<string>(), password)).Returns(PasswordVerificationResult.Failed);

            //UserNotFoundException exception = Assert.ThrowsAsync<UserNotFoundException>(() => localDatabaseAuthenticationService.LoginAsync(TradeProvider.TradeProviderType.Demo, expectedUsername, password));

            // string actualUsername = exception.Username;
            // Assert.AreEqual(expectedUsername, actualUsername);
        }

        // [Test]
        // public async Task Register_WithPasswordsNotMatching_ReturnsPasswordsDoNotMatch() {
        //     string password = "testpassword";
        //     string confirmPassword = "confirmtestpassword";
        //     RegistrationResult expected = RegistrationResult.PasswordsDoNotMatch;
        //
        //     RegistrationResult actual = await localDatabaseAuthenticationService.Register(It.IsAny<string>(), It.IsAny<string>(), password, confirmPassword);
        //
        //     Assert.AreEqual(expected, actual);
        // }

        // [Test]
        // public async Task Register_WithAlreadyExistingEmail_ReturnsEmailAlreadyExists() {
        //     string email = "test@gmail.com";
        //     _mockAccountService.Setup(s => s.GetByEmail(email)).ReturnsAsync(new Account());
        //     RegistrationResult expected = RegistrationResult.EmailAlreadyExists;
        //
        //     RegistrationResult actual = await localDatabaseAuthenticationService.Register(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());
        //
        //     Assert.AreEqual(expected, actual);
        // }
        //
        // [Test]
        // public async Task Register_WithAlreadyExistingUsername_ReturnsUsernameAlreadyExists() {
        //     string username = "testuser";
        //     _mockAccountService.Setup(s => s.GetByUsername(username)).ReturnsAsync(new Account());
        //     RegistrationResult expected = RegistrationResult.UsernameAlreadyExists;
        //
        //     RegistrationResult actual = await localDatabaseAuthenticationService.Register(It.IsAny<string>(), username, It.IsAny<string>(), It.IsAny<string>());
        //
        //     Assert.AreEqual(expected, actual);
        // }

        // [Test]
        // public async Task Register_WithNonExistingUserAndMatchingPasswords_ReturnsSuccess() {
        //     RegistrationResult expected = RegistrationResult.Success;
        //
        //     RegistrationResult actual = await localDatabaseAuthenticationService.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());
        //
        //     Assert.AreEqual(expected, actual);
        // }
    }
}