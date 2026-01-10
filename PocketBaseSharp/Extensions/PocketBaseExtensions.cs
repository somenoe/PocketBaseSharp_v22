using FluentResults;
using PocketBaseSharp.Models;

namespace PocketBaseSharp.Extensions
{
    public static class PocketBaseExtensions
    {

        public static Result<UserModel> GetCurrentUser(this PocketBase pocketBase)
        {
            if (pocketBase.AuthStore.Model is null || string.IsNullOrWhiteSpace(pocketBase.AuthStore.Model.Id))
            {
                return Result.Fail("AuthStore.Model is null or AuthStore.Model.Id is null");
            }

            return pocketBase.User.GetOne(pocketBase.AuthStore.Model.Id);
        }

        public static Task<Result<UserModel>> GetCurrentUserAsync(this PocketBase pocketBase)
        {
            if (pocketBase.AuthStore.Model is null || string.IsNullOrWhiteSpace(pocketBase.AuthStore.Model.Id))
            {
                var fail = Result.Fail($"AuthStore.Model is null or AuthStore.Model.Id is null");
                return Task.FromResult<Result<UserModel>>(fail);
            }

            return pocketBase.User.GetOneAsync(pocketBase.AuthStore.Model.Id);
        }

        public static Result<AdminModel> GetCurrentAdmin(this PocketBase pocketBase)
        {
            if (pocketBase.AuthStore.Model is null || string.IsNullOrWhiteSpace(pocketBase.AuthStore.Model.Id))
            {
                return Result.Fail("AuthStore.Model is null or AuthStore.Model.Id is null");
            }

            return pocketBase.Collection("_superusers").GetOne<AdminModel>(pocketBase.AuthStore.Model.Id);
        }

        public static Task<Result<AdminModel>> GetCurrentAdminAsync(this PocketBase pocketBase)
        {
            if (pocketBase.AuthStore.Model is null || string.IsNullOrWhiteSpace(pocketBase.AuthStore.Model.Id))
            {
                var fail = Result.Fail($"AuthStore.Model is null or AuthStore.Model.Id is null");
                return Task.FromResult<Result<AdminModel>>(fail);
            }

            return pocketBase.Collection("_superusers").GetOneAsync<AdminModel>(pocketBase.AuthStore.Model.Id);
        }
    }
}