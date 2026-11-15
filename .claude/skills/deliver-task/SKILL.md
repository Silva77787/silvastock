---
name: deliver-task
description: Deliver a SilvaStock GitHub task end to end with the develop → fresh-context review loop → PR workflow. Use when the user runs /deliver-task with a task ID such as F2-03 (or an issue number), or asks to deliver/implement a backlog task.
argument-hint: "<task-id or issue number> [more tasks…]  e.g. F2-03, 13, \"F2-03 F2-05\""
disable-model-invocation: true
---

# /deliver-task

You are the **main session** (orchestrator). Deliver the task(s) in `$ARGUMENTS` following the "Task workflow" in `CLAUDE.md`. Subagents cannot start other subagents, so you run every step. Report progress to the user in Portuguese (pt-PT); everything written to GitHub and to agents is in English.

Tools and constants:
- GitHub CLI: `GH="/c/Program Files/GitHub CLI/gh.exe"`, repo `Silva77787/silvastock`.
- Project board: number `5`, owner `Silva77787`, Status field `PVTSSF_lAHOA-ge2c4BrnK7zhp5FSI`, options Todo `f75ad846`, In Progress `47fc9ee4`, Done `98236657`.
- Git Bash PATH additions: `/c/Program Files/dotnet`, `/c/Program Files/nodejs`, `/c/Users/leona/AppData/Local/Programs/DockerDesktop/resources/bin`.

## 1. Resolve the task(s)
- For each argument: a task ID (`F2-03`, `DES-01`) → find the issue whose title starts with `[<ID>]`:
  `"$GH" issue list -R Silva77787/silvastock --state all --limit 200 --json number,title,state -q '.[] | select(.title | startswith("[F2-03]"))'`.
  A number (`13` or `#13`) → that issue.
- Stop and tell the user if the issue is not found, is closed, or is ambiguous.
- Read the issue (`issue view <n>`): goal, scope, acceptance criteria, dependencies, labels.
- Several tasks: run their loops in parallel only if they touch different files; otherwise run them one after another.

## 2. Check readiness (stop and ask the user if any fails)
- **Dependencies:** every task ID listed under "Dependencies" refers to a closed issue merged into `main`.
- **Contract:** from Phase 2 on, the phase's "Close the Phase N contract" issue (`FN-01`) is closed, unless this task *is* that issue.
- **Owner decisions:** the issue has no unresolved "Pending decision" section and no open item in `docs/DECISIONS.md` §12 that blocks it.
- **Ownership:** issues labelled `contract` or `infra`, or that need migrations, `.github/`, `Dockerfile` or `infra/` changes, are done by **you** (main session) on your own branch, still followed by the review loop (steps 5–7) and a PR you open yourself.
- **Summary-only specs** ("Summary spec" note): first expand the issue into a full spec (scope, folders, tests, edge cases) consistent with the docs, show it to the user, update the issue on approval, then continue.
- Update `main` (`git fetch origin && git switch main && git pull --ff-only`) and move the issue to **In Progress** on the board.

## 3. Pick the developer and the branch
- Labels/scope: `backend` (or `src/`, `tests/`) → `backend` agent; `frontend` (or `web/`) → `frontend` agent. A task needing both is split: backend first, then frontend.
- Branch: `feat/<task-id-lowercase>-<short-slug>` (e.g. `feat/f2-03-google-login`); use `fix/` or `chore/` when more fitting.
- **Sensitive task** (also needs `qa-security` review) if it touches authentication, sessions/cookies, authorisation, uploads, documents, public DTOs/endpoints, secrets or CORS.

## 4. Develop (fresh context)
Start the developer with the Agent tool: `subagent_type` = `backend`/`frontend`, `isolation: "worktree"`, `run_in_background: true`. Brief (English):
- the issue number and title, and "read it with gh issue view";
- the branch name to rename the worktree branch to (`git branch -m <branch>`), based on the current `origin/main`;
- allowed folders (from its agent definition and the issue);
- any decision already taken by the owner that is not yet in the docs;
- "Follow your agent instructions: develop, run all checks, commit, do not push, report."
Wait for the notification. Record the agent ID, worktree path, branch and commit from the result. If the report shows failing checks or a request for a contract/migration change, handle that first (do the change yourself or ask the user).

## 5. Review (new fresh agent every round)
Start a **new** `reviewer` agent (no worktree isolation needed; it only reads) with: the issue number, the developer's worktree path and branch, "review `git diff origin/main...HEAD` in that worktree", the round number, and in round ≥ 2 the previous findings plus the developer's responses (verbatim). For sensitive tasks start a **new** `qa-security` agent in parallel in Mode 1 (security review) with the same information. Do not add your own opinions about the code to the brief.

## 6. Fix
If any reviewer verdict is not `approve` (or critical/high/medium findings are open): send the findings verbatim to the **same** developer agent with SendMessage (`to` = its agent ID): "Fix round N: answer each finding by ID (fixed/declined with reason), re-run all checks, commit, do not push, report." Then go back to step 5 with a new reviewer.

Judge disagreements: if the developer declines a finding and the next reviewer rejects the decline, decide using the docs; if the docs don't settle it, ask the user.

## 7. Stop rule
- Done when every reviewer verdict is `approve` with no critical/high/medium findings open.
- **Max 3 review rounds.** If important findings remain after round 3, stop: summarise the open findings and both positions for the user, and wait.
- `block` verdict (docs contradiction, needs the owner): stop and ask the user.

## 8. Pull request
- Re-run the checks yourself in the developer's worktree (`dotnet build/test/format` or the `web/` scripts) and make sure the tree is clean. Never accept on the agent's word alone.
- Tell the developer (SendMessage): "Review approved. Push your branch and open the PR as described in your agent instructions; include these review rounds: …; remaining low findings: …". Pass the verdict summary of each round.
- Check the PR exists, then call `get_status`/`bind_pr` from the ccd_pr tools and read its CI. Do not poll CI. If CI fails, send the failure to the same developer agent for a fix round (counts as a review round only if code changes are substantial).
- Move nothing to Done yourself: merging (by the owner) closes the issue and the board's workflow sets Done.

## 9. Report to the user (pt-PT)
Short summary: the PR link, what was delivered, the review rounds (verdicts and main findings fixed), remaining low findings, CI state, and anything the owner must decide or do (merge, decisions). Offer the next task(s) whose dependencies are now satisfied.
