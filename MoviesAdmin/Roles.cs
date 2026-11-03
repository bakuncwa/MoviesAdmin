namespace MoviesAdmin
{
    // The two Auth0 roles (Auth0 Dashboard > User Management > Roles). The names must match Auth0
    // exactly: they arrive in the roles claim added by the "Add Roles to Token" post-login Action.
    //   [Authorize(Roles = Roles.Administrator)]   movie data CRUD
    //   [Authorize(Roles = Roles.Critic)]          writing reviews
    public static class Roles
    {
        public const string Administrator = "administrator";
        public const string Critic = "critic";

        // Either role, e.g. reading the movie catalog.
        public const string Any = Administrator + "," + Critic;
    }
}
