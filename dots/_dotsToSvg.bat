dot -Tsvg D:\myProgects\repLab\comp-lab1\dots\nfa.dot -o D:\myProgects\repLab\comp-lab1\dots\nfa.svg
dot -Tsvg D:\myProgects\repLab\comp-lab1\dots\dfa.dot -o D:\myProgects\repLab\comp-lab1\dots\dfa.svg
dot -Tsvg D:\myProgects\repLab\comp-lab1\dots\minDfa.dot -o D:\myProgects\repLab\comp-lab1\dots\minDfa.svg

@echo off
setlocal enabledelayedexpansion

set DOT_DIR=D:\myProgects\repLab\comp-lab1\dots
cd /d "%DOT_DIR%"

for %%f in (nfa_*.dot) do (
    set "input=%%f"
    set "output=%%~nf.svg"
    dot -Tsvg "!input!" -o "!output!"
)

for %%f in (dfa_*.dot) do (
    set "input=%%f"
    set "output=%%~nf.svg"
    dot -Tsvg "!input!" -o "!output!"
)

for %%f in (minDfa_*.dot) do (
    set "input=%%f"
    set "output=%%~nf.svg"
    dot -Tsvg "!input!" -o "!output!"
)