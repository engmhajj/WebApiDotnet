#!/usr/bin/env bash
set -e

_dev_commands="dotnet terraform kubectl all clean"

_dev_completion() {
    local cur_word opts
    COMPREPLY=()
    cur_word="${COMP_WORDS[COMP_CWORD]}"
    opts=$_dev_commands

    if [[ ${COMP_CWORD} -eq 1 ]]; then
        mapfile -t COMPREPLY < <(compgen -W "${opts}" -- "${cur_word}")
    fi

    return 0
}

complete -F _dev_completion ./dev.sh

echo "💻 Dev environment setup script"

usage() {
    echo ""
    echo "Usage: ./dev.sh [dotnet|terraform|kubectl|all|clean]"
    echo ""
    exit 1
}

case "$1" in
dotnet)
    ./init-dotnet-tools.sh
    ;;
terraform)
    ./init-terraform.sh
    ;;
kubectl)
    ./init-kubectl.sh
    ;;
all | "")
    ./init-dotnet-tools.sh
    ./init-terraform.sh
    ./init-kubectl.sh
    ;;
clean)
    echo "🧹 Cleaning up..."
    rm -rf ".terraform" "terraform.tfstate*" ".dotnet" ".config" ".kube"
    ;;
*)
    usage
    ;;
esac

echo "✅ Done."
