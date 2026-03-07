import "vuetify/styles";
import { createVuetify } from "vuetify";
import { aliases, mdi } from "vuetify/iconsets/mdi";
import "@mdi/font/css/materialdesignicons.css";

export const vuetify = createVuetify({
  icons: {
    defaultSet: "mdi",
    aliases,
    sets: { mdi },
  },
  theme: {
    defaultTheme: "darkPro",
    themes: {
      darkPro: {
        dark: true,
        colors: {
          background: "#0B1220",
          surface: "#0F1A2B",
          primary: "#7C3AED",
          secondary: "#06B6D4",
          success: "#22C55E",
          warning: "#F59E0B",
          error: "#EF4444",
          info: "#3B82F6",
        },
      },
    },
  },
  defaults: {
    VCard: { rounded: "xl", elevation: 2 },
    VBtn: { rounded: "lg", variant: "flat" },
    VTextField: { variant: "outlined", density: "comfortable" },
    VTextarea: { variant: "outlined", density: "comfortable" },
    VSelect: { variant: "outlined", density: "comfortable" },
  },
});
