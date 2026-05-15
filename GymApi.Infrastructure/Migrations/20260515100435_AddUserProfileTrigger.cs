using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Creates a Postgres trigger that automatically inserts a profile row
            // into public.users whenever Supabase creates a new auth.users record.
            // This keeps the domain User aggregate in sync with Supabase Auth
            // without requiring an explicit API call from the client.
            migrationBuilder.Sql("""
                create or replace function public.handle_new_user()
                returns trigger as $$
                begin
                    insert into public.users (id, email, name)
                    values (
                        new.id,
                        new.email,
                        coalesce(new.raw_user_meta_data->>'name', new.email)
                    );
                    return new;
                end;
                $$ language plpgsql security definer;

                create trigger on_auth_user_created
                    after insert on auth.users
                    for each row execute procedure public.handle_new_user();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                drop trigger if exists on_auth_user_created on auth.users;
                drop function if exists public.handle_new_user();
                """);
        }
    }
}
