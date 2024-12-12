import json
import boto3
import logging
from decimal import Decimal
from datetime import datetime
import jwt
from jwt.exceptions import InvalidTokenError
from boto3.dynamodb.conditions import Key

logger = logging.getLogger()
logger.setLevel(logging.INFO)

# Test configuration
IS_TESTING = True  # Set this to False in production
TEST_USER_ID = 'test-user-123'  # A predefined test user ID

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('MOFITWorkouts')

def validate_token(token, expected_user_id):
    # If in testing mode and using a predefined test token, bypass validation
    if IS_TESTING and token == 'test-token':
        return True

    try:
        # Remove 'Bearer ' prefix if it exists
        if token.startswith('Bearer '):
            token = token.split(' ')[1]
        
        # Decode and verify the JWT token
        decoded = jwt.decode(
            token,
            key='https://cognito-idp.us-west-1.amazonaws.com/us-west-1_wXwzuvOYr/.well-known/jwks.json',
            algorithms=['RS256'],
            options={
                "verify_signature": True,
                "verify_exp": True,
                "verify_aud": False
            }
        )
        
        logger.info(f"Decoded token: {json.dumps(decoded)}")
        
        # Additional user ID validation
        token_user_id = decoded.get('sub')
        if token_user_id != expected_user_id:
            logger.error(f"Token user ID {token_user_id} does not match expected {expected_user_id}")
            return False
        
        return True
        
    except InvalidTokenError as e:
        logger.error(f"Token validation failed: {str(e)}")
        return False
    except Exception as e:
        logger.error(f"Unexpected error validating token: {str(e)}")
        return False

def process_workout_data(workout_data):
    """Convert float values to Decimal for DynamoDB storage"""
    if isinstance(workout_data, dict):
        return {k: process_workout_data(v) for k, v in workout_data.items()}
    elif isinstance(workout_data, list):
        return [process_workout_data(item) for item in workout_data]
    elif isinstance(workout_data, float):
        return Decimal(str(workout_data))
    return workout_data

def lambda_handler(event, context):
    logger.info(f"Received event: {json.dumps(event)}")
    try:
        # Extract authorization header
        auth_header = event.get('headers', {}).get('Authorization') or event.get('headers', {}).get('authorization')
        
        # In testing mode, use a predefined test token and user ID
        if IS_TESTING:
            if not auth_header or auth_header != 'test-token':
                auth_header = 'test-token'
            path_user_id = TEST_USER_ID
        else:
            # Production validation
            if not auth_header:
                return {
                    'statusCode': 401,
                    'body': json.dumps({'message': 'No authorization token provided'})
                }
            
            # Get user ID from path parameters
            path_user_id = event['pathParameters']['userId']
        
        # Validate token
        if not validate_token(auth_header, path_user_id):
            return {
                'statusCode': 401,
                'body': json.dumps({'message': 'Invalid token or unauthorized'})
            }

        # Parse request body for POST method
        if event.get('body'):
            body = json.loads(event['body'])
            
            workout_data = {
                'UserId': path_user_id,
                'workoutId': f"{path_user_id}-{int(datetime.now().timestamp())}",
                'timestamp': datetime.utcnow().isoformat(),
                **body
            }
            
            # Process the data for DynamoDB
            processed_data = process_workout_data(workout_data)
            
            # Add workout to DynamoDB
            table.put_item(Item=processed_data)
            
            return {
                'statusCode': 200,
                'headers': {
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type,Authorization',
                    'Access-Control-Allow-Methods': 'POST,OPTIONS'
                },
                'body': json.dumps({
                    'message': 'Workout saved successfully',
                    'workoutId': workout_data['workoutId']
                })
            }

    except Exception as e:
        logger.error(f"Error: {str(e)}")
        return {
            'statusCode': 500,
            'body': json.dumps({'message': str(e)})
        }