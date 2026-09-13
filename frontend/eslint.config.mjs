import next from "eslint-config-next";

/**
 * eslint-config-next v16 ships a flat config array directly, so the FlatCompat bridge that
 * create-next-app scaffolds is no longer needed (and crashes on it).
 */
const eslintConfig = [
  {
    ignores: ["node_modules/**", ".next/**", "out/**", "build/**", "next-env.d.ts"],
  },
  ...next,
];

export default eslintConfig;
