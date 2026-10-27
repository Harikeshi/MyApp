//using System.Net;

//[Fact]
//public async Task AccountForgot_for_unknown_email_returns_404()
//{
//    var response = await factory.CreateClient()
//        .PostAsJsonAsync("/account/forgot", new { email = "nobody@example.com" });

//    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
//}

//[Fact]
//public async Task AccountForgot_for_confirmed_user_sends_reset_code()
//{
//    var email = await factory.CreateConfirmedUserAsync();

//    var response = await factory.CreateClient()
//        .PostAsJsonAsync("/account/forgot", new { email });

//    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//    Assert.True(factory.Emails.ResetCodes.ContainsKey(email));
//}