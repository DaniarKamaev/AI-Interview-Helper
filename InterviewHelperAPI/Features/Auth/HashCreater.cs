using System;
using System.Security.Cryptography;
using System.Text;

namespace InterviewHelperAPI.Features.Auth

{
    public static class HashCreater
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");

            using (var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                byte[] salt = algorithm.Salt;
                byte[] key = algorithm.GetBytes(KeySize);

                byte[] hashBytes = new byte[SaltSize + KeySize];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
                Buffer.BlockCopy(key, 0, hashBytes, SaltSize, KeySize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                if (hashBytes.Length != SaltSize + KeySize)
                    return false;

                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

                byte[] storedKey = new byte[KeySize];
                Buffer.BlockCopy(hashBytes, SaltSize, storedKey, 0, KeySize);

                using (var algorithm = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256))
                {
                    byte[] computedKey = algorithm.GetBytes(KeySize);

                    return CryptographicOperations.FixedTimeEquals(computedKey, storedKey);
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}