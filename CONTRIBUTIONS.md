# CONTRIBUTIONS

## Library Book Lending System

This document outlines the assigned responsibilities of each contributor and the shared responsibilities for developing, integrating, testing, and maintaining the Library Book Lending System.

## Kim

### Backend Modules
- Authentication and login.
- User and member management.
- Book management.
- Book reservation management.
- Borrowing management.
- Returns management.
- Reports and summary endpoints.

### Frontend Modules
- Core book management interface.
- Assigned core frontend functionality.
- Integration of assigned frontend functionality with the API.

## Cy

### Backend Modules
- Seat management.
- Seat reservation management.
- Reading-space management.
- Reading-space reservation management.
- QCPL external reservation monitoring.

### Frontend Modules
- Assigned additional frontend modules.
- Integration of assigned additional frontend functionality with the API.

## Shared Responsibilities

Both contributors are responsible for:

- Coordinating changes involving shared files.
- Integration of assigned modules.
- Testing API and frontend functionality.
- Reviewing and resolving integration issues.
- Coordinating database migrations.
- Maintaining project documentation.
- GitHub collaboration and version control.
- Ensuring the system complies with the IPT project requirements.

## Shared Files

The following files and project components require coordination:

- `Program.cs`
- `Data/LibraryDbContext.cs`
- `Data/DbSeeder.cs`
- `Migrations/`
- `wwwroot/index.html`
- `wwwroot/css/site.css`
- `wwwroot/js/app.js`
- `wwwroot/js/api.js`
- `README.md`
- `CONTRIBUTIONS.md`
- `.gitignore`
- `.editorconfig`
- `.gitattributes`

## Git Branch Workflow

- `main` – Stable version of the project.
- `dev` – Integration and testing branch.
- `kim` – Kim's assigned development branch.
- `cy` – Cy's assigned development branch.

Contributors work on their assigned branches and submit changes through pull requests for review and integration.

## Contribution Guidelines

- Contributors must use their own GitHub accounts for commits.
- Commit messages should clearly describe the changes.
- Assigned modules should be developed and tested before integration.
- Shared-file changes must be coordinated.
- Pull requests should be reviewed before merging.
- Contributions must reflect the actual work completed by each member.
- Secrets and unnecessary generated files must not be committed.

