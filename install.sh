#!/usr/bin/env sh
set -eu

repository="crisbraatz/MediaOrganizer"
install_dir="${MEDIAORGANIZER_INSTALL_DIR:-"$HOME/.mediaorganizer/bin"}"

command_exists() {
    command -v "$1" >/dev/null 2>&1
}

fail() {
    printf '%s\n' "MediaOrganizer install failed: $1" >&2
    exit 1
}

detect_asset() {
    os="$(uname -s)"
    arch="$(uname -m)"

    case "$os" in
        Linux)
            case "$arch" in
                x86_64|amd64)
                    printf '%s\n' "mediaorganizer-linux-x64.tar.gz"
                    ;;
                *)
                    fail "Linux architecture '$arch' is not supported yet."
                    ;;
            esac
            ;;
        Darwin)
            case "$arch" in
                arm64|aarch64)
                    printf '%s\n' "mediaorganizer-osx-arm64.tar.gz"
                    ;;
                x86_64|amd64)
                    printf '%s\n' "mediaorganizer-osx-x64.tar.gz"
                    ;;
                *)
                    fail "macOS architecture '$arch' is not supported yet."
                    ;;
            esac
            ;;
        *)
            fail "operating system '$os' is not supported by this installer."
            ;;
    esac
}

append_path_to_profile() {
    profile_file=""

    case "${SHELL:-}" in
        */zsh)
            profile_file="$HOME/.zshrc"
            ;;
        */bash)
            if [ "$(uname -s)" = "Darwin" ]; then
                profile_file="$HOME/.bash_profile"
            else
                profile_file="$HOME/.bashrc"
            fi
            ;;
        *)
            profile_file="$HOME/.profile"
            ;;
    esac

    marker="# Added by MediaOrganizer installer"
    path_line="export PATH=\"$install_dir:\$PATH\""

    if [ ! -f "$profile_file" ] || ! grep -F "$marker" "$profile_file" >/dev/null 2>&1; then
        {
            printf '\n%s\n' "$marker"
            printf '%s\n' "$path_line"
        } >> "$profile_file"
        printf '%s\n' "Added MediaOrganizer to PATH in $profile_file."
    fi
}

asset="$(detect_asset)"
download_url="https://github.com/$repository/releases/latest/download/$asset"
tmp_dir="$(mktemp -d)"

cleanup() {
    rm -rf "$tmp_dir"
}
trap cleanup EXIT INT TERM

command_exists curl || fail "curl is required."
command_exists tar || fail "tar is required."

printf '%s\n' "Installing MediaOrganizer..."
printf '%s\n' "Downloading $asset"

curl -fsSL "$download_url" -o "$tmp_dir/$asset" ||
    fail "could not download $download_url. Make sure a GitHub Release exists."

mkdir -p "$install_dir"
tar -xzf "$tmp_dir/$asset" -C "$install_dir"
chmod +x "$install_dir/mediaorganizer"

case ":$PATH:" in
    *":$install_dir:"*)
        ;;
    *)
        append_path_to_profile
        ;;
esac

printf '%s\n' "MediaOrganizer installed at $install_dir/mediaorganizer."

if command -v mediaorganizer >/dev/null 2>&1; then
    printf '%s\n' "Run it with: mediaorganizer"
else
    printf '%s\n' "Open a new terminal, then run: mediaorganizer"
fi
