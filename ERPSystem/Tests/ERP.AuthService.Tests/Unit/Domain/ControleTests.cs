using ERP.AuthService.Application.DTOs.Role;
using ERP.AuthService.Domain;
using FluentAssertions;

namespace ERP.AuthService.Tests.Unit.Domain
{
    public class ControleTests
    {
        // =========================
        // CONSTRUCTOR
        // =========================
        [Fact]
        public void Constructor_ValidInputs_ShouldCreateControle()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view all users");

            controle.Id.Should().NotBeEmpty();
            controle.Libelle.Should().Be("ViewUsers");
            controle.Description.Should().Be("Can view all users");
        }

        [Fact]
        public void Constructor_TwoControles_ShouldHaveDifferentIds()
        {
            var c1 = new Controle("UserManagement", "ViewUsers", "Can view users");
            var c2 = new Controle("UserManagement", "EditUsers", "Can edit users");
            c1.Id.Should().NotBe(c2.Id);
        }

        // =========================
        // UPDATE
        // =========================
        [Fact]
        public void Update_ValidInputs_ShouldUpdateFields()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            var request = new ControleRequestDto("UserManagement", "ManageUsers", "Can manage users");

            controle.Update(request);

            controle.Libelle.Should().Be("ManageUsers");
            controle.Description.Should().Be("Can manage users");
        }

        [Fact]
        public void Update_SameValues_ShouldNotUpdate()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            var originalLibelle = controle.Libelle;
            var originalDescription = controle.Description;

            controle.Update(new ControleRequestDto("UserManagement", "ViewUsers", "Can view users"));

            controle.Libelle.Should().Be(originalLibelle);
            controle.Description.Should().Be(originalDescription);
        }

        [Fact]
        public void Update_EmptyLibelle_ShouldKeepOriginalLibelle()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            controle.Update(new ControleRequestDto("UserManagement", "", "New description"));
            controle.Libelle.Should().Be("ViewUsers");
        }

        [Fact]
        public void Update_EmptyDescription_ShouldKeepOriginalDescription()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            controle.Update(new ControleRequestDto("UserManagement", "ViewUsers", ""));
            controle.Description.Should().Be("Can view users");
        }

        [Fact]
        public void Update_EmptyCategory_ShouldKeepOriginalCategory()
        {
            var controle = new Controle("UserManagement", "ViewUsers", "Can view users");
            controle.Update(new ControleRequestDto("", "ViewUsers", "Can view users"));
            controle.Category.Should().Be("UserManagement");
        }
    }
}