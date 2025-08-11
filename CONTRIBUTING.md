# Welcome to Game Guild contributing guide <!-- omit in#### Solve anScan through our [existing issues](https://github.com/gameguild-gg/website/issues) to find one that interests you. You can filter by labels such as:
- `good first issue` - Great for new contributors
- `feature` - New platform features
- `bug` - Platform issues to fix
- `documentation` - Docs improvements
- `community` - Community-focused features
- `playtesting` - Playtesting tools
- `education` - Educational content

As a general rule, we don't assign issues to anyone. If you find an issue to work on, you are welcome to open a PR with a fix.issue

Scan through our [existing issues](https://github.com/gameguild-gg/website/issues) to find one that interests you. You can filter by labels such as:
- `good first issue` - Great for new contributors
- `feature` - New platform features
- `bug` - Platform issues to fix
- `documentation` - Docs improvements
- `community` - Community-focused features
- `playtesting` - Playtesting tools
- `education` - Educational content

As a general rule, we don't assign issues to anyone. If you find an issue to work on, you are welcome to open a PR with a fix.c -->

Thank you for investing your time in contributing to our open source platform for game developers! Any contribution you make will help build a stronger community for game developers worldwide :sparkles:.

Read our [Code of Conduct](./CODE_OF_CONDUCT.md) to keep our community approachable and respectable.

In this guide you will get an overview of the contribution workflow from opening an issue, creating a PR, reviewing, and merging the PR. Whether you're contributing code, educational content, playtesting tools, or community features, your help is invaluable.

Use the table of contents icon <img alt="Table of contents icon" src="/contributing/images/table-of-contents.png" width="25" height="25" /> on the top left corner of this document to get to a specific section of this guide quickly.

## New contributor guide

To get an overview of the Game Guild platform, read the [README](../README.md) file. Game Guild is an open source platform designed to connect game developers through community collaboration, educational resources, and tools for playtesting and game launches.

Here are some resources to help you get started with open source contributions:

- [Finding ways to contribute to open source on GitHub](https://docs.github.com/en/get-started/exploring-projects-on-github/finding-ways-to-contribute-to-open-source-on-github)
- [Set up Git](https://docs.github.com/en/get-started/git-basics/set-up-git)
- [GitHub flow](https://docs.github.com/en/get-started/using-github/github-flow)
- [Collaborating with pull requests](https://docs.github.com/en/github/collaborating-with-pull-requests)

### Types of contributions we welcome

Game Guild benefits from various types of contributions:
- **Platform Features**: Core functionality for connecting developers
- **Educational Content**: Tutorials, guides, and learning resources
- **Playtesting Tools**: Features that help developers test their games
- **Launch Tools**: Resources to help developers publish and promote their games
- **Community Features**: Tools that enhance collaboration between developers
- **Documentation**: Help make our platform more accessible
- **Bug Reports & Fixes**: Keep the platform running smoothly


## Getting started

To navigate our codebase with confidence, familiarize yourself with our platform architecture. Game Guild consists of:
- **API Backend** (`/apps/api`): .NET Core API handling user management, project data, and platform services
- **Web Frontend** (`/apps/web`): Next.js application providing the user interface
- **Documentation** (`/docs`): Platform documentation and educational resources

Check the platform's current development priorities and see what types of contributions align with your interests. Some contributions don't even require writing code - you can help with documentation, educational content, or community features :sparkles:.

### Issues

#### Create a new issue

If you spot a problem with the platform, [search if an issue already exists](https://github.com/gameguild-gg/website/issues). Look for issues related to:
- Platform bugs or performance issues
- Missing features for game developers
- Educational content gaps
- Playtesting tool improvements
- Community collaboration features

If a related issue doesn't exist, you can open a new issue describing the problem or feature request.

#### Solve an issue

Scan through our [existing issues](https://github.com/github/docs/issues) to find one that interests you. You can narrow down the search using `labels` as filters. See "[Label reference](https://docs.github.com/en/contributing/collaborating-on-github-docs/label-reference)" for more information. As a general rule, we don’t assign issues to anyone. If you find an issue to work on, you are welcome to open a PR with a fix.

### Make Changes

#### Make changes in the UI

For small changes like typos, documentation fixes, or educational content updates, you can edit files directly in GitHub. Navigate to the file you want to edit and click the pencil icon to make your changes and [create a pull request](#pull-request) for review.

#### Make changes in a codespace

For more information about using a codespace for working on Game Guild, you can create a GitHub Codespace from the repository to get a full development environment in your browser.

#### Make changes locally

1. Fork the repository.
- Using GitHub Desktop:
  - [Getting started with GitHub Desktop](https://docs.github.com/en/desktop/installing-and-configuring-github-desktop/getting-started-with-github-desktop) will guide you through setting up Desktop.
  - Once Desktop is set up, you can use it to [fork the repo](https://docs.github.com/en/desktop/contributing-and-collaborating-using-github-desktop/cloning-and-forking-repositories-from-github-desktop)!

- Using the command line:
  - [Fork the repo](https://docs.github.com/en/github/getting-started-with-github/fork-a-repo#fork-an-example-repository) so that you can make your changes without affecting the original project until you're ready to merge them.

2. Set up your development environment:
   - Install **Node.js** (version specified in `.nvmrc` or `package.json`)
   - Install **.NET SDK** (for the API backend)
   - Install dependencies: `npm install`
   - Set up the database (see API documentation for details)

3. Create a working branch and start with your changes!

### Commit your update

Commit the changes once you are happy with them. Write clear, descriptive commit messages that explain what your changes do and why they're needed for the platform :zap:.

### Pull Request

When you're finished with the changes, create a pull request, also known as a PR.
- Fill the PR template to help reviewers understand your changes and their purpose for the Game Guild platform.
- Don't forget to [link PR to issue](https://docs.github.com/en/issues/tracking-your-work-with-issues/linking-a-pull-request-to-an-issue) if you are solving one.
- Enable the checkbox to [allow maintainer edits](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/allowing-changes-to-a-pull-request-branch-created-from-a-fork) so the branch can be updated for a merge.

Once you submit your PR, a Game Guild team member will review your proposal. We may ask questions or request additional information.
- We may ask for changes to be made before a PR can be merged, either using [suggested changes](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/incorporating-feedback-in-your-pull-request) or pull request comments. You can apply suggested changes directly through the UI. You can make any other changes in your fork, then commit them to your branch.
- As you update your PR and apply changes, mark each conversation as [resolved](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/commenting-on-a-pull-request#resolving-conversations).
- If you run into any merge issues, checkout this [git tutorial](https://github.com/skills/resolve-merge-conflicts) to help you resolve merge conflicts and other issues.

### Your PR is merged!

Congratulations :tada::tada: The Game Guild community thanks you :sparkles:.

Once your PR is merged, your contributions will be publicly visible and help game developers worldwide connect, learn, and collaborate.

Now that you are part of the Game Guild community, see how else you can contribute to building the best platform for game developers!

## Windows

Game Guild can be developed on Windows, however a few potential gotchas need to be kept in mind:

1. Regular Expressions: Windows uses `\r\n` for line endings, while Unix-based systems use `\n`. Therefore, when working on Regular Expressions, use `\r?\n` instead of `\n` in order to support both environments. The Node.js [`os.EOL`](https://nodejs.org/api/os.html#os_os_eol) property can be used to get an OS-specific end-of-line marker.
2. Paths: Windows systems use `\` for the path separator, which would be returned by `path.join` and others. You could use `path.posix`, `path.posix.join` etc and the [slash](https://ghub.io/slash) module, if you need forward slashes - like for constructing URLs - or ensure your code works with either.
3. Bash: Not every Windows developer has a terminal that fully supports Bash, so it's generally preferred to write scripts in JavaScript instead of Bash when possible.
4. Filename too long error: There is a 260 character limit for a filename when Git is compiled with `msys`. While the suggestions below are not guaranteed to work and could cause other issues, a few workarounds include:
    - Update Git configuration: `git config --system core.longpaths true`
    - Consider using a different Git client on Windows
