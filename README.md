# GitClearBranches

It clears all branches merged with the `HEAD` or **Active branch** and no longer has a remote branch.

### Installation

```bash
dotnet tool install --global GitClearBranches --version 1.0.1
```

### Usage

First run 

```bash
git fetch --all --prune
```

then

```bash
git rmb
```

### Parameters

* `-y` or `--yes` to skip the prompt for deleting branches

* any other branch name as parameter to make them an exception from getting removed

**PS:** The following branch names automatically get skipped:
  * master
  * main
  * dev
  * develop
  * prod
