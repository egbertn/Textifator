module.exports = {
    "env": {
        "browser": true,
        "es2021": true
    },
     "parser": "@typescript-eslint/parser",
    "extends": ["plugin:@typescript-eslint/recommended", "react-app"],
    "parserOptions": {
        "ecmaFeatures": {
            "jsx": true
        },
        "ecmaVersion": 12,
        "sourceType": "module"
    },
    "plugins": [
        "react", "@typescript-eslint"
    ],
    rules: {
        "@typescript-eslint/ban-types": [
            "error",
            {
                "extendDefaults": true,
                "types": {
                    "{}": false
                }
            }
        ],
        // suppress errors for missing 'import React' in files
        "react/react-in-jsx-scope": "off",
        // allow jsx syntax in js files (for next.js project)
        "react/jsx-filename-extension": [1, { "extensions": [".js", ".jsx", "tsx", ".ts"] }], //should add ".ts" if typescript project
    }
};
