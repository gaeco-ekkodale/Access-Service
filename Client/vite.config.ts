// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from '@tailwindcss/vite'
import federation from "@originjs/vite-plugin-federation";
import { ENV_KEYS } from "./env.d";

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const define: Record<string, string> = {};

  if (mode === "production") {
	//replace "disabled" with "production" to enable env injection
	// In production: use placeholders that will be replaced at Docker runtime
	ENV_KEYS.forEach((key) => {
	  const placeholder = `${key}_PLACEHOLDER`;
	  define[`import.meta.env.${key}`] = JSON.stringify(placeholder);
	});
  }
  // In development: Vite automatically loads from .env.development

  return {
	resolve: {
	  alias: {
		"@": "/src",
		"@api": "/src/api",
		"@utils": "/src/utils",
		"@shared": "/src/features/shared",
	  },
	},
	define,
	plugins: [
	  react(),
	  tailwindcss(),
	  federation({
		name: "access",
		filename: "remoteEntry.js",
		// Modules to expose
		exposes: {
		  "./App": "./src/App.tsx",
		},

		shared: [
		  "react",
		  "react-dom",
		  "react-router-dom",
		  "react-oidc-context",
		],
	  }),
	],
	build: {
	  target: "esnext",
	  minify: false,
	  rollupOptions: {
		input: {}, // No Entrys to avoid generating a classic bundle
	  },
	},
	server: {
	  host: "localhost",
	  port: 3000,
	  headers: {
		"Access-Control-Allow-Origin": "*",
	  },
	},
  };
});
