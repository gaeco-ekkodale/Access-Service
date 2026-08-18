// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Enum representing the various states of the Project Module.
 * This can be used to manage and display the module's UI based on its current status.
 */
export enum ProjectModuleState {
    /**
     * State representing that data is currently being loaded.
     */
    LOADING,

    /**
     * State representing an error occurred while fetching data.
     */
    ERROR,

    /**
     * State prompting the user to select a Use-Case and User Group.
     */
    SELECT_USER_GROUP_USE_CASE,

    /**
     * State indicating that there are no classifications available to display.
     */
    NO_DATA,

    /**
     * State representing a normal operational state where classifications are displayed.
     */
    NORMAL,
}
