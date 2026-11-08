#!/usr/bin/env bash
# Themes Auth0's hosted login page (Universal Login) to match Reelbox: the earth-tone colors from
# wwwroot/css/site.css, the wordmark at wwwroot/images/reelbox-logo.svg, the "Reelbox Admin" name
# instead of the tenant id, and a Reelbox title on the login form. Everything goes through the
# Auth0 Management API, so re-running it resets the login page to this look.
#
# Needs the Auth0 CLI, logged in to the tenant once:
#   brew tap auth0/auth0-cli && brew install auth0
#   auth0 login --scopes "read:branding,update:branding,create:branding,update:tenant_settings,update:prompts"
#
# Usage (from the repository root): ./scripts/auth0-branding.sh [logo-url]
# The logo must be on a public URL, so it defaults to the copy served by the Azure deployment.
set -euo pipefail

logo_url="${1:-https://reelbox-admin-0350f5.azurewebsites.net/images/reelbox-logo.svg}"

# Colors from site.css (light theme).
accent="#2f6b3a"      # --ma-accent, forest green
accent_hover="#24552d"
background="#faf7f0"  # --ma-bg, warm white
surface="#ffffff"     # --ma-surface
sand="#efe7d8"        # --ma-surface-2
text="#2b1d14"        # --ma-text, dark brown
muted="#6f5b4b"       # --ma-muted
border="#d9cdbd"      # --ma-border flattened onto the warm-white background
danger="#b5442c"      # --ma-danger, terracotta

if ! command -v auth0 >/dev/null; then
  echo "The Auth0 CLI isn't installed: brew tap auth0/auth0-cli && brew install auth0" >&2
  exit 1
fi

echo "Branding the Auth0 login page (logo: $logo_url)"

# Name and logo the login page shows for this tenant ("Log in to Reelbox Admin ...").
auth0 api patch "tenants/settings" --data "{
  \"friendly_name\": \"Reelbox Admin\",
  \"picture_url\": \"$logo_url\"
}" >/dev/null
echo "  tenant name and logo"

auth0 api patch "branding" --data "{
  \"logo_url\": \"$logo_url\",
  \"colors\": { \"primary\": \"$accent\", \"page_background\": \"$background\" }
}" >/dev/null
echo "  brand colors"

# The full Universal Login theme: every color, the rounded corners Reelbox's buttons and cards
# use, and the logo centred above the form.
theme="{
  \"displayName\": \"Reelbox\",
  \"borders\": {
    \"button_border_radius\": 10, \"button_border_weight\": 1, \"buttons_style\": \"rounded\",
    \"input_border_radius\": 10, \"input_border_weight\": 1, \"inputs_style\": \"rounded\",
    \"show_widget_shadow\": true, \"widget_border_weight\": 1, \"widget_corner_radius\": 16
  },
  \"colors\": {
    \"base_focus_color\": \"$accent\", \"base_hover_color\": \"$accent_hover\",
    \"body_text\": \"$text\", \"captcha_widget_theme\": \"light\", \"error\": \"$danger\",
    \"header\": \"$text\", \"icons\": \"$muted\", \"input_background\": \"$surface\",
    \"input_border\": \"$border\", \"input_filled_text\": \"$text\",
    \"input_labels_placeholders\": \"$muted\", \"links_focused_components\": \"$accent\",
    \"primary_button\": \"$accent\", \"primary_button_label\": \"#ffffff\",
    \"secondary_button_border\": \"$border\", \"secondary_button_label\": \"$text\",
    \"success\": \"$accent\", \"widget_background\": \"$surface\", \"widget_border\": \"$sand\"
  },
  \"fonts\": {
    \"font_url\": \"\", \"links_style\": \"normal\", \"reference_text_size\": 16,
    \"body_text\": { \"bold\": false, \"size\": 87.5 },
    \"buttons_text\": { \"bold\": true, \"size\": 100 },
    \"input_labels\": { \"bold\": false, \"size\": 100 },
    \"links\": { \"bold\": true, \"size\": 87.5 },
    \"subtitle\": { \"bold\": false, \"size\": 87.5 },
    \"title\": { \"bold\": true, \"size\": 150 }
  },
  \"page_background\": {
    \"background_color\": \"$background\", \"background_image_url\": \"\", \"page_layout\": \"center\"
  },
  \"widget\": {
    \"header_text_alignment\": \"center\", \"logo_height\": 44, \"logo_position\": \"center\",
    \"logo_url\": \"$logo_url\", \"social_buttons_layout\": \"bottom\"
  }
}"

# A tenant has at most one theme: update it if there is one, otherwise create it.
theme_id=$(auth0 api get "branding/themes/default" 2>/dev/null | sed -n 's/.*"themeId": *"\([^"]*\)".*/\1/p' | head -1 || true)
if [[ -n "$theme_id" ]]; then
  auth0 api patch "branding/themes/$theme_id" --data "$theme" >/dev/null
else
  auth0 api post "branding/themes" --data "$theme" >/dev/null
fi
echo "  login page theme"

# Title and subtitle on the login form, for both the password screen and the email-first screen.
login_text='{
  "title": "Welcome to Reelbox",
  "description": "Log in to manage the movie catalog or write your reviews."
}'
auth0 api put "prompts/login/custom-text/en" --data "{ \"login\": $login_text }" >/dev/null
auth0 api put "prompts/login-id/custom-text/en" --data "{ \"login-id\": $login_text }" >/dev/null
echo "  login page text"

echo "Done. Open the app and click Log in to see it."
