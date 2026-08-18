// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AccessRightDTO } from '../models/AccessRightDTO';
import type { CommitAccessRightsRequestDTO } from '../models/CommitAccessRightsRequestDTO';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AccessRightsService {
    /**
     * Adds a new list of access rights to the database.
     * @param requestBody
     * @returns AccessRightDTO Created
     * @throws ApiError
     */
    public static createAccessRightsAsync(
        requestBody?: Array<AccessRightDTO>,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/AccessRights',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Updates a list of access rights to the database.
     * @param requestBody
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static updateAccessRightsAsync(
        requestBody?: Array<AccessRightDTO>,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/AccessRights',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Deletes a list of access rights to the database.
     * @param requestBody
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static deleteAccessRightsAsync(
        requestBody?: Array<AccessRightDTO>,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/AccessRights',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets all access rights.
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAllAccessRightsAsync(): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights',
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Commits the final list of access rights for the specified use case and user group.
     * @param useCaseId
     * @param userGroupId
     * @param requestBody
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static commitAccessRightsAsync(
        useCaseId: string,
        userGroupId: string,
        requestBody?: CommitAccessRightsRequestDTO,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/AccessRights/usecase/{useCaseId}/usergroup/{userGroupId}/commit',
            path: {
                'useCaseId': useCaseId,
                'userGroupId': userGroupId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `Bad Request`,
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets an access right by its ID.
     * @param id
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAccessRightAsync(
        id: string,
    ): CancelablePromise<AccessRightDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights/{id}',
            path: {
                'id': id,
            },
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets access rights by use case ID.
     * @param useCaseId
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAccessRightsByUseCaseAsync(
        useCaseId: string,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights/usecase/{useCaseId}',
            path: {
                'useCaseId': useCaseId,
            },
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets access rights by user group ID.
     * @param userGroupId
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAccessRightsByUserGroupAsync(
        userGroupId: string,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights/usergroup/{userGroupId}',
            path: {
                'userGroupId': userGroupId,
            },
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets access rights by use case ID and user group ID.
     * @param useCaseId
     * @param userGroupId
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAccessRightsByUseCaseUserGroupAsync(
        useCaseId: string,
        userGroupId: string,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights/usecase/{useCaseId}/usergroup/{userGroupId}',
            path: {
                'useCaseId': useCaseId,
                'userGroupId': userGroupId,
            },
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Gets access rights by use case ID, user group ID, and classification ID.
     * @param useCaseId
     * @param userGroupId
     * @param classificationId
     * @returns AccessRightDTO OK
     * @throws ApiError
     */
    public static getAccessRightsByUseCaseUserGroupClassificationAsync(
        useCaseId: string,
        userGroupId: string,
        classificationId: string,
    ): CancelablePromise<Array<AccessRightDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AccessRights/usecase/{useCaseId}/usergroup/{userGroupId}/classification/{classificationId}',
            path: {
                'useCaseId': useCaseId,
                'userGroupId': userGroupId,
                'classificationId': classificationId,
            },
            errors: {
                401: `Unauthorized`,
                500: `Internal Server Error`,
            },
        });
    }
}
