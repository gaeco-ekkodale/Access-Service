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
import type { UseCaseDB } from '../models/UseCaseDB'
import type { CancelablePromise } from '../core/CancelablePromise'
import { OpenAPI } from '../core/OpenAPI'
import { request as __request } from '../core/request'
export class UseCasesService {
	/**
	 * @returns UseCaseDB OK
	 * @throws ApiError
	 */
	public static getApiUseCases(): CancelablePromise<Array<UseCaseDB>> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/UseCases',
			errors: {
				404: `Not Found`,
				500: `Internal Server Error`,
			},
		})
	}
	/**
	 * @param name
	 * @param description
	 * @returns any Created
	 * @throws ApiError
	 */
	public static postApiUseCases(name?: string, description?: string): CancelablePromise<any> {
		return __request(OpenAPI, {
			method: 'POST',
			url: '/api/UseCases',
			query: {
				name: name,
				description: description,
			},
			errors: {
				400: `Bad Request`,
				500: `Internal Server Error`,
			},
		})
	}
	/**
	 * @param id
	 * @param name
	 * @param description
	 * @returns any OK
	 * @throws ApiError
	 */
	public static putApiUseCases(id?: string, name?: string, description?: string): CancelablePromise<any> {
		return __request(OpenAPI, {
			method: 'PUT',
			url: '/api/UseCases',
			query: {
				id: id,
				name: name,
				description: description,
			},
			errors: {
				400: `Bad Request`,
				404: `Not Found`,
				500: `Internal Server Error`,
			},
		})
	}
	/**
	 * @param id
	 * @returns UseCaseDB OK
	 * @throws ApiError
	 */
	public static getApiUseCases1(id: string): CancelablePromise<UseCaseDB> {
		return __request(OpenAPI, {
			method: 'GET',
			url: '/api/UseCases/{id}',
			path: {
				id: id,
			},
			errors: {
				404: `Not Found`,
				500: `Internal Server Error`,
			},
		})
	}
	/**
	 * @param id
	 * @returns any OK
	 * @throws ApiError
	 */
	public static deleteApiUseCases(id: string): CancelablePromise<any> {
		return __request(OpenAPI, {
			method: 'DELETE',
			url: '/api/UseCases/{id}',
			path: {
				id: id,
			},
			errors: {
				404: `Not Found`,
				500: `Internal Server Error`,
			},
		})
	}
}
