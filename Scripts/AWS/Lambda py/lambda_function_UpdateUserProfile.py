import json
import boto3
from botocore.exceptions import ClientError
from decimal import Decimal

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('MOFITUserProfiles')

# Custom encoder to handle Decimal types
class DecimalEncoder(json.JSONEncoder):
    def default(self, o):
        if isinstance(o, Decimal):
            return float(o)
        return super(DecimalEncoder, self).default(o)

def lambda_handler(event, context):
    try:
        # Extract userId from the event
        user_id = event['pathParameters']['userId']
        
        # Parse the request body
        body = json.loads(event['body'])
        
        # Prepare the update expression and attribute values
        update_expression = "SET "
        expression_attribute_values = {}
        expression_attribute_names = {}
        
        for key, value in body.items():
            if key != 'UserId':  # Skip UserId as it's the partition key
                # Replace spaces with underscores in the attribute name
                safe_key = key.replace(" ", "_")
                update_expression += f"#{safe_key} = :{safe_key}, "
                expression_attribute_values[f':{safe_key}'] = value
                expression_attribute_names[f'#{safe_key}'] = key
        
        # Remove the trailing comma and space
        update_expression = update_expression[:-2]
        
        # Update the item in DynamoDB
        response = table.update_item(
            Key={'UserId': user_id},
            UpdateExpression=update_expression,
            ExpressionAttributeValues=expression_attribute_values,
            ExpressionAttributeNames=expression_attribute_names,
            ReturnValues="ALL_NEW"
        )
        
        # Convert response to JSON serializable format
        attributes = response['Attributes']
        return {
            'statusCode': 200,
            'body': json.dumps(attributes, cls=DecimalEncoder)
        }
    except KeyError as e:
        return {
            'statusCode': 400,
            'body': json.dumps({'message': f"Missing required parameter: {str(e)}"})
        }
    except json.JSONDecodeError:
        return {
            'statusCode': 400,
            'body': json.dumps({'message': "Invalid JSON in request body"})
        }
    except ClientError as e:
        return {
            'statusCode': 500,
            'body': json.dumps({'message': str(e)})
        }
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps({'message': f"Unexpected error: {str(e)}"})
        }