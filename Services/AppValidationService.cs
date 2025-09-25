namespace ScspApi.Services
{
    public class AppValidationService
    {
        private readonly List<string> _validTokens;

        public AppValidationService()
        {
            // Tokens 
            _validTokens = new List<string>
            {
                "SbImj@0ork'9SuKtDI2x]hfr6Tt.6Cd7sz8v3mpGKbM[JCe", 
            };
        }

        public bool IsValidToken(string token)
        {
            return _validTokens.Contains(token);
        }
    }

}
