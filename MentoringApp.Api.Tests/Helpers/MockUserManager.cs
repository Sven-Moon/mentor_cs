using Microsoft.AspNetCore.Identity;
using Moq;

namespace MentoringApp.Api.Tests.Helpers
{
    public static class MockUserManager<TUser> where TUser : class
    {
        public static Mock<UserManager<TUser>> Create()
        {
            var store = new Mock<IUserStore<TUser>>();

            return new Mock<UserManager<TUser>>(
                    store.Object,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!,
                    null!
            );
        }
    }
}
