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
            // NOTE: The user profile trigger (handle_new_user / on_auth_user_created)
            // must be created manually in the Supabase SQL Editor.
            // It cannot be applied via EF migrations due to Supabase's auth schema restrictions.
            
            // This trigger inserts a profile row into public.users whenever Supabase creates a new auth.users record.
            /*
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
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.Sql("""
                drop trigger if exists on_auth_user_created on auth.users;
                drop function if exists public.handle_new_user();
                """);
            */
        }
    }
}
