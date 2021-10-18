using RegisterClient;

namespace EpiasRest
{
    public static class RegisterDbHelper
    {

        public static RegisterDbClient GetClient()
        {

            var client = new RegisterDbClient()
            {
                Host = Parameters.RegisterDbHost,
                Port = Parameters.RegisterDbPort,
                Logger = new RegisterModels.Logger(false)
            };
            client.Logger.ExternalLogMethod =
                (s, e) =>
                {
                    Helper.log.WriteLogLine(e.ToString());
                };
            client.Connect();
            return client;
        }
    }
}
