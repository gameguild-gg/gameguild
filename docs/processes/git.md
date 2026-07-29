# Git Workflow

## Overview

The full lifecycle: feature flows into `develop`, `develop` flows into `release`, `release` flows into both `main` (tagged) and `develop`, and `hotfix` branches from `main` flow back into both `main` (tagged) and `develop`.

```mermaid
gitGraph
   commit id: "init"
   branch develop
   checkout develop
   branch feat/new-feat
   checkout feat/new-feat
   commit id: "wip"
   commit id: "done"
   checkout develop
   merge feat/new-feat
   branch release/x.y.z
   checkout release/x.y.z
   commit id: "bugfix"
   commit id: "polish"
   checkout develop
   merge release/x.y.z
   checkout main
   merge release/x.y.z tag: "x.y.z"
   branch hotfix/x.y.z
   checkout hotfix/x.y.z
   commit id: "urgent fix"
   checkout main
   merge hotfix/x.y.z tag: "x.y.z"
   checkout develop
   merge hotfix/x.y.z
```

## Branches

- **main** - the main branch, where the production code is stored. 
    - the ci/cd pipeline publishes the code to the **production** environment;
    - if the ci/cd doesnt compile or the tests fail, the commit should be reverted automatically, so the error should never be propagated forward.
    - this branch should be merged from **releases/** branches or **hotfix/** branches;
- **develop** - the development branch, where new features are integrated. 
    - the ci/cd pipeline publishes the code to the **dev** environment.
- **releases/** - release branches, where the code is ready to be released
    - it is originated from the **develop** branch;
- **feature/** - feature branches, where new features are developed
    - it can be originated from either the **develop** branch or any other feature branch;
- **bugfix/** - bugfix branches, where bugs are fixed
    - it is originated from the **develop** branch;
- **hotfix/** - hotfix branches, where urgent fixes are applied
    - it is originated from the **main** branch;

## Creating and merging feature branches

You can create a feature branch from the **develop** branch or any other feature branch.

``` mermaid
gitGraph TB:
    branch develop
    commit
    branch feature/1
    commit id: "feature/1"
    branch feature/2
    commit id: "feature/2"
    checkout develop
    merge feature/1
    commit id: "develop"
    merge feature/2
    commit id: "develop"
```

## Bugfix branches

You can create a bugfix branch from the **develop** branch.

``` mermaid
gitGraph TB:
    branch develop
    commit
    branch bugfix/1
    commit id: "bugfix/1"
    checkout develop
    merge bugfix/1
    commit id: "develop"
```

## Hotfix branches

You can create a hotfix branch from the **main** branch, and merge it back to the **main** branch, and optionally apply the fix to the **develop** branch as well.

``` mermaid
gitGraph TB:
    commit
    branch develop
    checkout main
    commit
    branch hotfix/1
    commit id: "hotfix/1"
    checkout main
    merge hotfix/1
    commit id: "main updated"
    checkout develop
    merge hotfix/1
    commit id: "develop updated"
```

## Releases branches

You can create a release branch from the **develop** branch.

``` mermaid
gitGraph TB:
    branch develop
    checkout main
    commit
    checkout develop
    commit
    branch release/1.0.0
    commit id: "release/1.0.0"
    checkout main
    merge release/1.0.0
    commit id: "main"
```

## Branch deletion

- **feature/** branches should be deleted after merging to **develop**
- **bugfix/** branches should be deleted after merging to **develop**
- **hotfix/** branches should be deleted after merging to **main** and **develop**
- **release/** branches should be deleted after merging to **main** (and **develop** if applicable)